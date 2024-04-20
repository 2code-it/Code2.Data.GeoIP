using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class GeoIPService<Tblock, Tlocation, Tisp> : IGeoIPService<Tblock, Tlocation, Tisp>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
		where Tisp : IspBase, new()
	{
		public GeoIPService() : this(GetDefaultOptions()) { }
		public GeoIPService(GeoIPServiceOptions options) :
			this(options, new InMemoryBlocksRepository<Tblock>(), new InMemoryLocationsRepository<Tlocation>(), new InMemoryIspsRepository<Tisp>())
		{ }
		public GeoIPService(GeoIPServiceOptions options, IRepository<Tblock, UInt128> blocksRepository, IRepository<Tlocation, int> locationsRepository, IRepository<Tisp, int> ispsRepository) :
			this(options, blocksRepository, locationsRepository, ispsRepository, new NetworkUtility(), new CsvUpdateService(), new FileSystem(), new CsvReaderFactory())
		{ }

		internal GeoIPService(
			GeoIPServiceOptions options,
			IRepository<Tblock, UInt128> blocksRepository,
			IRepository<Tlocation, int> locationsRespository,
			IRepository<Tisp, int> ispsRepository,
			INetworkUtility networkUtility,
			ICsvUpdateService csvUpdateService,
			IFileSystem fileSystem,
			ICsvReaderFactory csvReaderFactory
		)
		{
			_blocksRepository = blocksRepository;
			_locationsRepository = locationsRespository;
			_ispsRepository = ispsRepository;
			_networkUtility = networkUtility;
			_csvUpdateService = csvUpdateService;
			_fileSystem = fileSystem;
			_csvReaderFactory = csvReaderFactory;

			_csvUpdateService.Update += async (_, _) => { Update?.Invoke(this, EventArgs.Empty); await LoadAsync(); };
			_csvUpdateService.Error += (_, e) => { OnError((Exception)e.ExceptionObject); };

			Configure(options);
		}


		private readonly IRepository<Tblock, UInt128> _blocksRepository;
		private readonly IRepository<Tlocation, int> _locationsRepository;
		private readonly IRepository<Tisp, int> _ispsRepository;
		private readonly INetworkUtility _networkUtility;
		private readonly IFileSystem _fileSystem;
		private readonly ICsvReaderFactory _csvReaderFactory;
		private readonly ICsvUpdateService _csvUpdateService;
		private readonly object _lock = new object();

		public event EventHandler<UnhandledExceptionEventArgs>? Error;
		public event EventHandler? Update;

		public GeoIPServiceOptions Options { get; private set; } = GetDefaultOptions();
		public bool HasData => _blocksRepository.HasData;
		public bool IsUpdating => _csvUpdateService.IsUpdating;

		public Tblock? GetBlock(UInt128 ipNumber)
		{
			lock (_lock)
			{
				return _blocksRepository.GetSingle(ipNumber);
			}
		}

		public Tblock? GetBlock(string ipAddress)
			=> GetBlock(_networkUtility.GetIpNumberFromAddress(ipAddress));

		public IEnumerable<Tblock> GetBlocks(Func<Tblock, bool> filter)
		{
			lock (_lock)
			{
				return _blocksRepository.Get(filter);
			}
		}

		public Tlocation? GetLocation(int geoNameId)
		{
			lock (_lock)
			{
				return _locationsRepository.GetSingle(geoNameId);
			}
		}

		public IEnumerable<Tlocation> GetLocations(Func<Tlocation, bool> filter)
		{
			lock (_lock)
			{
				return _locationsRepository.Get(filter);
			}
		}

		public Tisp? GetIsp(int ispId)
		{
			lock (_lock)
			{
				return _ispsRepository.GetSingle(ispId);
			}
		}

		public IEnumerable<Tisp> GetIsps(Func<Tisp, bool> filter)
		{
			lock (_lock)
			{
				return _ispsRepository.Get(filter);
			}
		}

		public async Task UpdateFilesAsync()
		{
			ThrowOnInvalidUpdateOption();
			await _csvUpdateService.UpdateFilesAsync();
		}

		public async Task LoadAsync()
			=> await Task.Run(Load);

		public void Configure(Action<GeoIPServiceOptions> configure)
		{
			GeoIPServiceOptions options = new GeoIPServiceOptions();
			configure(options);
			Configure(options);
		}

		public void Configure(GeoIPServiceOptions options)
		{
			if (options.CsvBlocksIPv4FileFilter is not null) Options.CsvBlocksIPv4FileFilter = options.CsvBlocksIPv4FileFilter;
			if (options.CsvBlocksIPv6FileFilter is not null) Options.CsvBlocksIPv6FileFilter = options.CsvBlocksIPv6FileFilter;
			if (options.CsvLocationsFileFilter is not null) Options.CsvLocationsFileFilter = options.CsvLocationsFileFilter;
			if (options.CsvDataDirectory is not null) Options.CsvDataDirectory = options.CsvDataDirectory;
			if (options.CsvDownloadUrl is not null) Options.CsvDownloadUrl = options.CsvDownloadUrl;
			if (options.CsvReaderErrorLogFile is not null) Options.CsvReaderErrorLogFile = options.CsvReaderErrorLogFile;
			if (options.MaxmindEdition is not null) Options.MaxmindEdition = options.MaxmindEdition;
			if (options.MaxmindLicenseKey is not null) Options.MaxmindLicenseKey = options.MaxmindLicenseKey;
			if (options.CsvReaderChunkSize > 0) Options.CsvReaderChunkSize = options.CsvReaderChunkSize;
			Options.UseDownloadHashCheck = options.UseDownloadHashCheck;
			Options.AutoUpdate = options.AutoUpdate;
			Options.AutoLoad = options.AutoLoad;

			_csvUpdateService.UseDownloadHashCheck = Options.UseDownloadHashCheck;
			_csvUpdateService.CsvFileFilters = new[] { Options.CsvBlocksIPv4FileFilter, Options.CsvBlocksIPv6FileFilter, Options.CsvLocationsFileFilter }.Where(x => x != null).ToArray()!;
			_csvUpdateService.CsvDataDirectory = Options.CsvDataDirectory!;
			_csvUpdateService.CsvDownloadUrl = Options.CsvDownloadUrl!;
			_csvUpdateService.MaxmindEdition = Options.MaxmindEdition!;
			_csvUpdateService.MaxmindLicenseKey = Options.MaxmindLicenseKey!;
			if (Options.AutoUpdate)
			{
				ThrowOnInvalidUpdateOption();
				_csvUpdateService.StartAutomaticUpdating();
			}
			if (Options.AutoLoad)
			{
				if (HasData) return;
				if (Options.AutoUpdate && (_csvUpdateService.IsUpdating || _csvUpdateService.GetFileLastWriteTime() == DateTime.MinValue)) return;
				Load();
			}
		}

		private void Load()
		{
			lock (_lock)
			{
				var filePaths = GetCsvFilePaths();

				if (filePaths.BlocksIPv4 is null && filePaths.BlocksIPv6 is null)
					OnError("Csv blocks file not found");

				if (_blocksRepository.HasData) _blocksRepository.RemoveAll();
				if (_locationsRepository.HasData) _locationsRepository.RemoveAll();
				if (Options.CsvReaderErrorLogFile is not null)
				{
					string logFilePath = _fileSystem.PathGetFullPath(Options.CsvReaderErrorLogFile);
					if (_fileSystem.FileExists(logFilePath)) _fileSystem.FileDelete(logFilePath);
				}

				AddCsvFileToRepository(_blocksRepository, filePaths.BlocksIPv4, Options.CsvReaderChunkSize);
				AddCsvFileToRepository(_blocksRepository, filePaths.BlocksIPv6, Options.CsvReaderChunkSize);
				AddCsvFileToRepository(_locationsRepository, filePaths.Locations, Options.CsvReaderChunkSize);
				AddCsvFileToRepository(_ispsRepository, filePaths.Isps, Options.CsvReaderChunkSize);
			}
		}

		private void EnsureDataDirectoryExists()
		{
			string dataDir = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);
			_fileSystem.DirectoryCreate(dataDir);
		}

		private CsvFilePaths GetCsvFilePaths()
		{
			EnsureDataDirectoryExists();
			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);
			string[] files = _fileSystem.DirectoryGetFiles(dataDirectory, "*.*");
			CsvFilePaths filePaths = new CsvFilePaths();
			filePaths.BlocksIPv4 = string.IsNullOrEmpty(Options.CsvBlocksIPv4FileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv4FileFilter));
			filePaths.BlocksIPv6 = string.IsNullOrEmpty(Options.CsvBlocksIPv6FileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv6FileFilter));
			filePaths.Locations = string.IsNullOrEmpty(Options.CsvLocationsFileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvLocationsFileFilter));
			filePaths.Isps = string.IsNullOrEmpty(Options.CsvIspFileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvIspFileFilter));
			return filePaths;
		}

		private void ThrowOnInvalidUpdateOption()
		{
			ThrowOnEmptyRequiredOption(Options.MaxmindLicenseKey, nameof(Options.MaxmindLicenseKey));
			ThrowOnEmptyRequiredOption(Options.MaxmindEdition, nameof(Options.MaxmindEdition));
			ThrowOnEmptyRequiredOption(Options.CsvDownloadUrl, nameof(Options.CsvDownloadUrl));
			if (string.IsNullOrEmpty(Options.CsvBlocksIPv4FileFilter) && string.IsNullOrEmpty(Options.CsvBlocksIPv6FileFilter) && string.IsNullOrEmpty(Options.CsvLocationsFileFilter))
			{
				OnError("Updating requires atleast one file filter");
			}
		}

		private void ThrowOnEmptyRequiredOption(string? optionValue, string optionName)
		{
			if (!string.IsNullOrEmpty(optionValue)) return;
			OnError($"Required option not set {optionName}");
		}

		private void UpdateSubnet(IEnumerable<ISubnet> items)
		{
			Parallel.ForEach(items, x =>
			{
				var range = _networkUtility.GetRangeFromCidr(x.Network);
				x.BeginAddress = range.begin;
				x.EndAddress = range.end;
			});
		}

		private void AddCsvFileToRepository<T, Tid>(IRepository<T, Tid> repository, string? filePath, int chunkSize) where T : class, new()
		{
			if (filePath is null) return;

			filePath = _fileSystem.PathGetFullPath(filePath);
			using StreamReader reader = _fileSystem.FileOpenText(filePath);
			ICsvReader<T> csvReader = _csvReaderFactory.Create<T>(reader);
			csvReader.Options.IgnoreEmptyWhenDeserializing = true;
			csvReader.Options.Header = csvReader.ReadLine();
			List<string> errorList = new List<string>();
			if (Options.CsvReaderErrorLogFile is not null)
			{
				csvReader.Error += (object? sender, UnhandledExceptionEventArgs args) => { errorList.Add(((Exception)args.ExceptionObject).Message); };
			}

			while (!csvReader.EndOfStream)
			{
				T[] items = csvReader.ReadObjects(chunkSize);
				if (items.Length == 0) continue;
				if (items[0] is ISubnet) UpdateSubnet(items.Select(x => (ISubnet)x!));
				repository.Add(items);
			}

			if (errorList.Count > 0)
			{
				errorList.Insert(0, $"=={filePath}==");
				string logFilePath = _fileSystem.PathGetFullPath(Options.CsvReaderErrorLogFile!);
				_fileSystem.FileAppendAllLines(logFilePath, errorList);
			}
		}

		private void OnError(string message)
			=> OnError(new InvalidOperationException(message));

		private void OnError(Exception exception)
		{
			if (Error is null) throw exception;
			Error.Invoke(this, new UnhandledExceptionEventArgs(exception, false));
		}

		private static GeoIPServiceOptions GetDefaultOptions()
		{
			using Stream stream = typeof(GeoIPServiceOptions).Assembly.GetManifestResourceStream(typeof(GeoIPServiceOptions), $"{nameof(GeoIPServiceOptions)}.json")!;
			return JsonSerializer.Deserialize<GeoIPServiceOptions>(stream)!;
		}

		private class CsvFilePaths
		{
			public string? BlocksIPv4 { get; set; }
			public string? BlocksIPv6 { get; set; }
			public string? Locations { get; set; }
			public string? Isps { get; set; }
		}
	}
}
