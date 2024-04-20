using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class GeoIPServiceTests : IDisposable
	{
		private GeoIPServiceOptions _options = default!;
		private IRepository<BlockBase, UInt128> _blocksRepository = default!;
		private IRepository<LocationBase, int> _locationsRepository = default!;
		private IRepository<IspBase, int> _ispsRepository = default!;
		private INetworkUtility _networkUtility = default!;
		private ICsvReaderFactory _csvReaderFactory = default!;
		private CsvReaderOptions _csvReaderOptions = default!;
		private ICsvReader<BlockBase> _csvReaderBlock = default!;
		private ICsvReader<LocationBase> _csvReaderLocation = default!;
		private ICsvUpdateService _csvUpdateService = default!;
		private IFileSystem _fileSystem = default!;
		private MemoryStream _memoryStream = default!;
		private StreamReader _streamReader = default!;


		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void Load_When_WithoutBlockFileFilterOptionsDefined_Expect_Exception()
		{
			var service = GetGeoIPService();
			service.Options.CsvBlocksIPv4FileFilter = null;
			service.Options.CsvBlocksIPv6FileFilter = null;
			service.LoadAsync().Wait();
		}

		[TestMethod]
		public void Load_When_WithFileFilterOptionsDefined_Expect_FileSystemCallForEach()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv", "some-blocksipv6.csv", "some-locations.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";
			service.Options.CsvBlocksIPv6FileFilter = "blocksipv6.csv";
			service.Options.CsvLocationsFileFilter = "locations.csv";

			service.LoadAsync().Wait();

			_fileSystem.Received(3).FileOpenText(Arg.Any<string>());
		}

		[TestMethod]
		public void Load_When_WithFileFilterOptionsIPv4OnlyDefined_Expect_FileSystemCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";

			service.LoadAsync().Wait();

			_fileSystem.Received(1).FileOpenText(Arg.Any<string>());
		}

		[TestMethod]
		public void Load_When_WithBlocksFileFilterOptionDefined_Expect_CsvReaderCall()
		{
			var service = GetGeoIPService();
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "some-blocksipv4.csv" });
			service.Options.CsvBlocksIPv4FileFilter = "blocksipv4.csv";

			service.LoadAsync().Wait();

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

			service.LoadAsync().Wait();

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

			service.LoadAsync().Wait();

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

			service.LoadAsync().Wait();

			_networkUtility.Received(1).GetRangeFromCidr(Arg.Is(network));
		}

		private GeoIPService<BlockBase, LocationBase, IspBase> GetGeoIPService(bool resetDependencies = true)
		{
			if (resetDependencies) ResetDependencies();
			return new GeoIPService<BlockBase, LocationBase, IspBase>(_options, _blocksRepository, _locationsRepository, _ispsRepository, _networkUtility, _csvUpdateService, _fileSystem, _csvReaderFactory);
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
			_csvUpdateService = Substitute.For<ICsvUpdateService>();
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