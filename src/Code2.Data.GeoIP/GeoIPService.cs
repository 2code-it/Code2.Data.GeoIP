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
	public class GeoIPService<Tblock, Tlocation> : IGeoIPService<Tblock, Tlocation>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		public GeoIPService() : this(GetDefaultOptions()) { }
		public GeoIPService(GeoIPServiceOptions options) :
			this(options, new InMemoryBlocksRepository<Tblock>(), new InMemoryLocationsRepository<Tlocation>())
		{ }
		public GeoIPService(GeoIPServiceOptions options, IRepository<Tblock, UInt128> blocksRepository, IRepository<Tlocation, int> locationsRepository) :
			this(options, blocksRepository, locationsRepository, new NetworkUtility(), new CsvUpdateService(), new FileSystem(), new CsvReaderFactory())
		{ }

		internal GeoIPService(
			GeoIPServiceOptions options,
			IRepository<Tblock, UInt128> blocksRepository,
			IRepository<Tlocation, int> locationsRespository,
			INetworkUtility networkUtility,
			ICsvUpdateService csvUpdateService,
			IFileSystem fileSystem,
			ICsvReaderFactory csvReaderFactory
		)
		{
			_blocksRepository = blocksRepository;
			_locationsRepository = locationsRespository;
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
				var files = GetCsvFilePaths();

				if (files.ipv4block is null && files.ipv6block is null)
					OnError("Csv blocks file not found");

				if (_blocksRepository.HasData) _blocksRepository.RemoveAll();
				if (_locationsRepository.HasData) _locationsRepository.RemoveAll();
				if (Options.CsvReaderErrorLogFile is not null)
				{
					string logFilePath = _fileSystem.PathGetFullPath(Options.CsvReaderErrorLogFile);
					if (_fileSystem.FileExists(logFilePath)) _fileSystem.FileDelete(logFilePath);
				}

				AddCsvFileToRepository(_blocksRepository, files.ipv4block, Options.CsvReaderChunkSize);
				AddCsvFileToRepository(_blocksRepository, files.ipv6block, Options.CsvReaderChunkSize);
				AddCsvFileToRepository(_locationsRepository, files.locations, Options.CsvReaderChunkSize);
			}
		}

		private void EnsureDataDirectoryExists()
		{
			string dataDir = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);
			_fileSystem.DirectoryCreate(dataDir);
		}

		private (string? ipv4block, string? ipv6block, string? locations) GetCsvFilePaths()
		{
			EnsureDataDirectoryExists();
			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);
			string[] files = _fileSystem.DirectoryGetFiles(dataDirectory, "*.*");
			string? ipv4BlocksFilePath = string.IsNullOrEmpty(Options.CsvBlocksIPv4FileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv4FileFilter));
			string? ipv6BlocksFilePath = string.IsNullOrEmpty(Options.CsvBlocksIPv6FileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv6FileFilter));
			string? locationsFilePath = string.IsNullOrEmpty(Options.CsvLocationsFileFilter) ? null : files.FirstOrDefault(x => x.Contains(Options.CsvLocationsFileFilter));
			return (ipv4BlocksFilePath, ipv6BlocksFilePath, locationsFilePath);
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
	}
}
