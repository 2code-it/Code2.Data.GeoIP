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
			Options = options ?? GetDefaultOptions();
		}

		private readonly IRepository<Tblock, UInt128> _blocksRepository;
		private readonly IRepository<Tlocation, int> _locationsRepository;
		private readonly INetworkUtility _networkUtility;
		private readonly IFileSystem _fileSystem;
		private readonly ICsvReaderFactory _csvReaderFactory;

		public GeoIPServiceOptions Options { get; private set; }

		public Tblock? GetBlock(UInt128 ipNumber)
			=> _blocksRepository.GetSingle(ipNumber);

		public Tblock? GetBlock(string ipAddress)
			=> GetBlock(_networkUtility.GetIpNumberFromAddress(ipAddress));

		public Tlocation? GetLocation(int geoNameId)
			=> _locationsRepository.GetSingle(geoNameId);


		public void Load()
		{
			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory);
			string[] files = _fileSystem.DirectoryGetFiles(dataDirectory, "*.*");
			string? ipv4BlocksFilePath = Options.CsvBlocksIPv4FileFilter is null ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv4FileFilter));
			string? ipv6BlocksFilePath = Options.CsvBlocksIPv6FileFilter is null ? null : files.FirstOrDefault(x => x.Contains(Options.CsvBlocksIPv6FileFilter));
			string? locationsFilePath = Options.CsvLocationsFileFilter is null ? null : files.FirstOrDefault(x => x.Contains(Options.CsvLocationsFileFilter));

			if (ipv4BlocksFilePath is null && ipv6BlocksFilePath is null)
				throw new InvalidOperationException("Csv blocks file not found");

			if (_blocksRepository.HasData) _blocksRepository.RemoveAll();
			if (_locationsRepository.HasData) _locationsRepository.RemoveAll();

			AddCsvFileToRepository(_blocksRepository, ipv4BlocksFilePath, Options.CsvReaderChunkSize);
			AddCsvFileToRepository(_blocksRepository, ipv6BlocksFilePath, Options.CsvReaderChunkSize);
			AddCsvFileToRepository(_locationsRepository, locationsFilePath, Options.CsvReaderChunkSize);
		}

		public void UpdateFiles()
		{
			ThrowOnEmptyRequiredOption(Options.MaxmindLicenseKey, nameof(Options.MaxmindLicenseKey));
			ThrowOnEmptyRequiredOption(Options.MaxmindEdition, nameof(Options.MaxmindEdition));
			ThrowOnEmptyRequiredOption(Options.CsvDownloadUrl, nameof(Options.CsvDownloadUrl));

			string urlZip = Options.CsvDownloadUrl.Replace($"$({nameof(Options.MaxmindLicenseKey)})", Options.MaxmindLicenseKey)
				.Replace($"$({nameof(Options.MaxmindEdition)})", Options.MaxmindEdition);

			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory);
			_fileSystem.DirectoryCreate(dataDirectory);
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
			string dataDirectory = _fileSystem.PathGetFullPath(Options.CsvDataDirectory);

			ExtractZipEntryIfExists(zipFilePath, Options.CsvBlocksIPv4FileFilter, dataDirectory);
			ExtractZipEntryIfExists(zipFilePath, Options.CsvBlocksIPv6FileFilter, dataDirectory);
			ExtractZipEntryIfExists(zipFilePath, Options.CsvLocationsFileFilter, dataDirectory);
		}

		public async Task LoadAsync()
			=> await Task.Run(Load);

		public async Task UpdateFilesAsync()
			=> await Task.Run(UpdateFiles);

		public async Task UpdateFilesAsync(string zipFilePath)
			=> await Task.Run(() => UpdateFiles(zipFilePath));

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

		private void AddCsvFileToRepository<T, Tid>(IRepository<T, Tid> repository, string? filePath, int chunkSize) where T: class, new()
		{
			if (filePath is null) return;

			filePath = _fileSystem.PathGetFullPath(filePath);
			using StreamReader reader = _fileSystem.FileOpenText(filePath);
			ICsvReader<T> csvReader = _csvReaderFactory.Create<T>(reader);
			csvReader.Options.Header = csvReader.ReadLine();
			while (!csvReader.EndOfStream)
			{
				T[] items = csvReader.ReadObjects(chunkSize);
				if (items.Length == 0) continue;
				if (items[0] is ISubnet) UpdateSubnet(items.Select(x => (ISubnet)x!));
				repository.Add(items);
			}
		}

		public GeoIPServiceOptions GetDefaultOptions()
		{
			string filePath = _fileSystem.PathGetFullPath($"./{nameof(GeoIPServiceOptions)}.json");
			string json = _fileSystem.FileReadAllText(filePath);
			return JsonSerializer.Deserialize<GeoIPServiceOptions>(json)!;
		}
	}
}
