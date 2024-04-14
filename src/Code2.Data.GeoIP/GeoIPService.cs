using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class GeoIPService<Tblock, Tlocation> : IGeoIPService<Tblock, Tlocation>, IDisposable
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		public GeoIPService() : this(null) { }
		public GeoIPService(GeoIPServiceOptions? options) :
			this(options, new InMemoryBlocksRepository<Tblock>(), new InMemoryLocationsRepository<Tlocation>())
		{ }
		public GeoIPService(GeoIPServiceOptions? options, IRepository<Tblock, UInt128> blocksRepository, IRepository<Tlocation, int> locationsRepository) :
			this(options, blocksRepository, locationsRepository, new NetworkUtility(), new FileSystem(), new CsvReaderFactory())
		{ }

		internal GeoIPService(
			GeoIPServiceOptions? options,
			IRepository<Tblock, UInt128> blocksRepository,
			IRepository<Tlocation, int> locationsRespository,
			INetworkUtility networkUtility,
			IFileSystem fileSystem,
			ICsvReaderFactory csvReaderFactory
		)
		{
			_blocksRepository = blocksRepository;
			_locationsRepository = locationsRespository;
			_networkUtility = networkUtility;
			_fileSystem = fileSystem;
			_csvReaderFactory = csvReaderFactory;
			if (options is not null) Configure(options);
		}

		private readonly IRepository<Tblock, UInt128> _blocksRepository;
		private readonly IRepository<Tlocation, int> _locationsRepository;
		private readonly INetworkUtility _networkUtility;
		private readonly IFileSystem _fileSystem;
		private readonly ICsvReaderFactory _csvReaderFactory;
		private readonly object _lock = new object();
		private Timer? _updateTimer;
		private const int _msPerHour = 3600000;

		public GeoIPServiceOptions Options { get; private set; } = GetDefaultOptions();
		public bool HasData => _blocksRepository.HasData;

		public Tblock? GetBlock(UInt128 ipNumber)
		{
			lock (_lock)
			{
				return _blocksRepository.GetSingle(ipNumber);
			}
		}

		public Tblock? GetBlock(string ipAddress)
			=> GetBlock(_networkUtility.GetIpNumberFromAddress(ipAddress));

		public Tlocation? GetLocation(int geoNameId)
		{
			lock (_lock)
			{
				return _locationsRepository.GetSingle(geoNameId);
			}
		}


		public void Load()
		{
			lock (_lock)
			{
				var files = GetCsvFilePaths();

				if (files.ipv4block is null && files.ipv6block is null)
					throw new InvalidOperationException("Csv blocks file not found");

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

		public void UpdateFiles()
		{
			ThrowOnEmptyRequiredOption(Options.MaxmindLicenseKey, nameof(Options.MaxmindLicenseKey));
			ThrowOnEmptyRequiredOption(Options.MaxmindEdition, nameof(Options.MaxmindEdition));
			ThrowOnEmptyRequiredOption(Options.CsvDownloadUrl, nameof(Options.CsvDownloadUrl));

			string urlZip = Options.CsvDownloadUrl!.Replace($"$({nameof(Options.MaxmindLicenseKey)})", Options.MaxmindLicenseKey)
				.Replace($"$({nameof(Options.MaxmindEdition)})", Options.MaxmindEdition);

			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);
			string downloadFilePath = _fileSystem.PathCombine(dataDirectory, $"{Options.MaxmindEdition}.zip");

			if (_fileSystem.FileExists(downloadFilePath)) _fileSystem.FileDelete(downloadFilePath);

			using Stream stream = _networkUtility.HttpGetStream(urlZip);
			using Stream fileStream = _fileSystem.FileCreate(downloadFilePath);
			{
				stream.CopyTo(fileStream);
				fileStream.Position = 0;

				if (Options.UseDownloadHashCheck)
				{
					string hashRemote = _networkUtility.HttpGetString(urlZip + ".sha256");
					hashRemote = hashRemote.Split(' ').First().ToUpper();
					string hashLocal = _fileSystem.FileGetSha256Hex(fileStream);
					if (hashRemote != hashLocal)
						throw new InvalidOperationException("File hash mismatch");
				}
				fileStream.Close();
			}

			UpdateFiles(downloadFilePath);
			_fileSystem.FileDelete(downloadFilePath);
		}

		public void UpdateFiles(string zipFilePath)
		{
			EnsureDataDirectoryExists();
			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory!);

			ExtractZipEntryIfExists(zipFilePath, Options.CsvBlocksIPv4FileFilter, dataDirectory);
			ExtractZipEntryIfExists(zipFilePath, Options.CsvBlocksIPv6FileFilter, dataDirectory);
			ExtractZipEntryIfExists(zipFilePath, Options.CsvLocationsFileFilter, dataDirectory);
		}

		public DateTime GetLastFileWriteTime()
		{
			var files = GetCsvFilePaths();
			string[] fileArray = new[] { files.ipv4block, files.ipv6block, files.locations }.Where(x => x is not null).ToArray()!;
			if (fileArray.Length == 0) return DateTime.MinValue;
			return fileArray.Select(x => _fileSystem.FileGetLastWriteTime(x!)).Max();
		}

		public async Task LoadAsync()
			=> await Task.Run(Load);

		public async Task UpdateFilesAsync()
			=> await Task.Run(UpdateFiles);

		public async Task UpdateFilesAsync(string zipFilePath)
			=> await Task.Run(() => UpdateFiles(zipFilePath));

		public void Configure(Action<GeoIPServiceOptions> configure)
		{
			configure(Options);
			PostConfigure();
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
			Options.CsvUpdateIntervalInDays = options.CsvUpdateIntervalInDays;
			Options.AutoLoad = options.AutoLoad;

			PostConfigure();
		}

		private void PostConfigure()
		{
			if (Options.CsvUpdateIntervalInDays > 0)
			{
				StartUpdateTimer();
			}
			else
			{
				StopUpdateTimer();
			}

			if (Options.AutoLoad) AutoLoad();
		}

		private void StartUpdateTimer()
		{
			lock (_lock)
			{
				if (_updateTimer is not null) return;
				_updateTimer = new Timer(new TimerCallback(OnUpdateTimerTick), null, _msPerHour, _msPerHour);
			}
		}

		private void StopUpdateTimer()
		{
			lock (_lock)
			{
				if (_updateTimer is null) return;
				_updateTimer.Dispose();
				_updateTimer = null;
			}
		}

		private async void OnUpdateTimerTick(object? state)
		{
			if ((DateTime.Now - GetLastFileWriteTime()).TotalDays <= Options.CsvUpdateIntervalInDays) return;
			await UpdateFilesAsync();
			await LoadAsync();
		}

		private async void AutoLoad()
		{
			if (HasData) return;
			if (Options.CsvUpdateIntervalInDays > 0 && (DateTime.Now - GetLastFileWriteTime()).TotalDays <= Options.CsvUpdateIntervalInDays)
			{
				await UpdateFilesAsync();
			}
			await LoadAsync();
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

		private void ExtractZipEntryIfExists(string zipFilePath, string? nameFilter, string destinationDirectory)
		{
			if (string.IsNullOrEmpty(nameFilter)) return;
			_fileSystem.ZipArchiveExtractEntryTo(zipFilePath, nameFilter, destinationDirectory);
		}

		private void ThrowOnEmptyRequiredOption(string? optionValue, string optionName)
		{
			if (!string.IsNullOrEmpty(optionValue)) return;
			throw new InvalidOperationException($"Required option not set {optionName}");
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

		private static GeoIPServiceOptions GetDefaultOptions()
		{
			using Stream stream = typeof(GeoIPServiceOptions).Assembly.GetManifestResourceStream(typeof(GeoIPServiceOptions), $"{nameof(GeoIPServiceOptions)}.json")!;
			return JsonSerializer.Deserialize<GeoIPServiceOptions>(stream)!;
		}

		public void Dispose()
		{
			StopUpdateTimer();
		}
	}
}
