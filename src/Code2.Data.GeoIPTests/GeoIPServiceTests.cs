using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Code2.Data.GeoIPTests
{
	[TestClass]
	public class GeoIPServiceTests : IDisposable
	{
		private GeoIPServiceOptions _options = default!;
		private IRepository<BlockBase, UInt128> _blocksRepository = default!;
		private IRepository<LocationBase, int> _locationsRepository = default!;
		private INetworkUtility _networkUtility = default!;
		private ICsvReaderFactory _csvReaderFactory = default!;
		private CsvReaderOptions _csvReaderOptions = default!;
		private ICsvReader<BlockBase> _csvReaderBlock = default!;
		private ICsvReader<LocationBase> _csvReaderLocation = default!;
		private IFileSystem _fileSystem = default!;
		private MemoryStream _memoryStream = default!;
		private StreamReader _streamReader = default!;


		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Load_When_WithoutBlockFileOptionsDefined_Expect_Exception()
		{
			var service = GetGeoIPService();
			service.Options.CsvBlocksIPv4FileFilter = null;
			service.Options.CsvBlocksIPv6FileFilter = null;
			service.Load();
		}

		[TestMethod]
		public void Load_When_WithFileOptionsDefined_Expect_FileSystemCallForEach()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv", "some-blocksipv6.csv", "some-locations.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			service.Options.CsvBlocksIPv6FileFilter = "blocksipv6.csv";
			service.Options.CsvLocationsFileFilter = "locations.csv";

			service.Load();

			_fileSystem.Received(3).FileOpenText(Arg.Any<string>());
		}

		[TestMethod]
		public void Load_When_WithFileOptionsIPv4OnlyDefined_Expect_FileSystemCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";

			service.Load();

			_fileSystem.Received(1).FileOpenText(Arg.Any<string>());
		}

		[TestMethod]
		public void Load_When_WithBlocksFileOptionDefined_Expect_CsvReaderCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";

			service.Load();

			_csvReaderBlock.Received(1).ReadObjects(Arg.Any<int>());
		}

		[TestMethod]
		public void Load_When_BlocksFileWithBlocks_Expect_BlocksRepositoryAddCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			BlockBase block = new BlockBase() { Network = "0.0.0.1/24" };
			_csvReaderBlock.ReadObjects(Arg.Any<int>()).Returns(new[] { block });

			service.Load();

			_blocksRepository.Received(1).Add(Arg.Any<IEnumerable<BlockBase>>());
		}

		[TestMethod]
		public void Load_When_LocationsFileWithLocations_Expect_LocationsRepositoryAddCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv", "some-locations.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			service.Options.CsvLocationsFileFilter = "locations.csv";
			LocationBase location = new();
			_csvReaderLocation.ReadObjects(Arg.Any<int>()).Returns(new[] { location });

			service.Load();

			_locationsRepository.Received(1).Add(Arg.Any<IEnumerable<LocationBase>>());
		}

		[TestMethod]
		public void Load_When_BlocksFileWithBlocks_Expect_NetworkUtilityCidrCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			string network = "0.0.0.1/24";
			BlockBase block = new BlockBase() { Network = network };
			_csvReaderBlock.ReadObjects(Arg.Any<int>()).Returns(new[] { block });

			service.Load();

			_networkUtility.Received(1).GetRangeFromCidr(Arg.Is(network));
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void UpdateFiles_When_RequiredOptionNotSet_Expect_Exception()
		{
			var service = GetGeoIPService();

			service.Load();
		}

		[TestMethod]
		public void UpdateFiles_When_RequiredOptionsSet_Expect_ParametersInUrl()
		{
			using Stream httpStream = new MemoryStream();
			using Stream fileStream = new MemoryStream();

			var service = GetGeoIPService();
			service.Options.MaxmindEdition = "edition1";
			service.Options.MaxmindLicenseKey = "key1";
			service.Options.CsvDownloadUrl = $"$({nameof(GeoIPServiceOptions.MaxmindEdition)})$({nameof(GeoIPServiceOptions.MaxmindLicenseKey)})";
			service.Options.UseDownloadHashCheck = false;
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(x => fileStream);

			string url = "";
			_networkUtility.HttpGetStream(Arg.Do<string>(x => url = x)).Returns(x => httpStream);

			service.UpdateFiles();

			string expectedUrl = $"{service.Options.MaxmindEdition}{service.Options.MaxmindLicenseKey}";
			Assert.AreEqual(expectedUrl, url);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void UpdateFiles_When_UseDownloadHashCheckIsTrueAndHashMismatch_Expect_Exception()
		{
			using Stream httpStream = new MemoryStream();
			using Stream fileStream = new MemoryStream();

			var service = GetGeoIPService();
			service.Options.MaxmindEdition = "edition1";
			service.Options.MaxmindLicenseKey = "key1";
			service.Options.CsvDownloadUrl = "/";
			service.Options.UseDownloadHashCheck = true;
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(x => fileStream);
			_networkUtility.HttpGetStream(Arg.Any<string>()).Returns(x => httpStream);
			_networkUtility.HttpGetString(Arg.Any<string>()).Returns("ab10ef ?");
			_fileSystem.FileGetSha256Hex(Arg.Any<Stream>()).Returns("AADD00");

			service.UpdateFiles();
		}

		[TestMethod]
		public void UpdateFiles_When_UseDownloadHashCheckFalseAndHashMisMatch_Expect_ZipExtractEntryCall()
		{
			using Stream httpStream = new MemoryStream();
			using Stream fileStream = new MemoryStream();

			var service = GetGeoIPService();
			service.Options.MaxmindEdition = "edition1";
			service.Options.MaxmindLicenseKey = "key1";
			service.Options.CsvDownloadUrl = "/";
			service.Options.UseDownloadHashCheck = false;
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(x => fileStream);
			_networkUtility.HttpGetStream(Arg.Any<string>()).Returns(x => httpStream);
			_networkUtility.HttpGetString(Arg.Any<string>()).Returns("aadd00 ?");
			_fileSystem.FileGetSha256Hex(Arg.Any<Stream>()).Returns("AADD01");

			service.UpdateFiles();

			_fileSystem.Received(1).ZipArchiveExtractEntryTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
		}

		[TestMethod]
		public void UpdateFiles_When_UseDownloadHashCheckIsTrueHashMatch_Expect_ZipExtractEntryCalls()
		{
			using Stream httpStream = new MemoryStream();
			using Stream fileStream = new MemoryStream();

			var service = GetGeoIPService();
			service.Options.MaxmindEdition = "edition1";
			service.Options.MaxmindLicenseKey = "key1";
			service.Options.CsvDownloadUrl = "/";
			service.Options.UseDownloadHashCheck = true;
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv", "some-blocksipv6.csv", "some-locations.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			service.Options.CsvBlocksIPv6FileFilter = "blocksipv6.csv";
			service.Options.CsvLocationsFileFilter = "locations.csv";
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(x => fileStream);
			_networkUtility.HttpGetStream(Arg.Any<string>()).Returns(x => httpStream);
			_networkUtility.HttpGetString(Arg.Any<string>()).Returns("aadd00 ?");
			_fileSystem.FileGetSha256Hex(Arg.Any<Stream>()).Returns("AADD00");

			service.UpdateFiles();

			_fileSystem.Received(3).ZipArchiveExtractEntryTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
		}

		[TestMethod]
		public void UpdateFiles_When_DownloadFileExists_Expect_FileDeleteCallTwice()
		{
			using Stream httpStream = new MemoryStream();
			using Stream fileStream = new MemoryStream();

			var service = GetGeoIPService();
			service.Options.MaxmindEdition = "edition1";
			service.Options.MaxmindLicenseKey = "key1";
			service.Options.CsvDownloadUrl = "/";
			service.Options.UseDownloadHashCheck = false;
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(x => fileStream);
			_networkUtility.HttpGetStream(Arg.Any<string>()).Returns(x => httpStream);
			_fileSystem.FileExists(Arg.Any<string>()).Returns(true);

			service.UpdateFiles();

			_fileSystem.Received(2).FileDelete(Arg.Any<string>());
		}

		private GeoIPService<BlockBase, LocationBase> GetGeoIPService(bool resetDependencies = true)
		{
			if (resetDependencies) ResetDependencies();
			return new GeoIPService<BlockBase, LocationBase>(_options, _blocksRepository, _locationsRepository, _networkUtility, _fileSystem, _csvReaderFactory);
		}

		private void ResetDependencies()
		{
			_options = new();

			_blocksRepository = Substitute.For<IRepository<BlockBase, UInt128>>();
			_locationsRepository = Substitute.For<IRepository<LocationBase, int>>();
			_networkUtility = Substitute.For<INetworkUtility>();
			_fileSystem = Substitute.For<IFileSystem>();
			_memoryStream = new MemoryStream();
			_streamReader = new StreamReader(_memoryStream);
			_fileSystem.FileOpenText(Arg.Any<string>()).Returns(_streamReader);
			_csvReaderFactory = Substitute.For<ICsvReaderFactory>();
			_csvReaderOptions = new CsvReaderOptions();
			_csvReaderBlock = Substitute.For<ICsvReader<BlockBase>>();
			_csvReaderLocation = Substitute.For<ICsvReader<LocationBase>>();
			_csvReaderBlock.Options.Returns(_csvReaderOptions);
			_csvReaderLocation.Options.Returns(_csvReaderOptions);
			int a = 0;
			int b = 0;
			_csvReaderBlock.EndOfStream.Returns(x => { a++; return a == 1 ? false : true; });
			_csvReaderLocation.EndOfStream.Returns(x => { b++; return b == 1 ? false : true; });
			_csvReaderFactory.Create<BlockBase>(Arg.Any<TextReader>()).Returns(_csvReaderBlock);
			_csvReaderFactory.Create<LocationBase>(Arg.Any<TextReader>()).Returns(_csvReaderLocation);
		}

		public void Dispose()
		{
			_memoryStream?.Dispose();
			_streamReader?.Dispose();
		}
	}
}