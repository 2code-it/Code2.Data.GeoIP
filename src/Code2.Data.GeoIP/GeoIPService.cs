using Code2.Tools.Csv;
using Code2.Data.GeoIP.Internals;

namespace Code2.Data.GeoIP
{
    public class GeoIPService<Tblock, Tlocation> : IGeoIPService<Tblock, Tlocation>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		public GeoIPService() : this(new GeoIPServiceOptions()) { }
		public GeoIPService(GeoIPServiceOptions options) :
			this(options, new InMemoryBlocksRepository<Tblock>(), new InMemoryLocationsRepository<Tlocation>())
		{ }
		public GeoIPService(GeoIPServiceOptions options, IRepository<Tblock,UInt128> blocksRepository, IRepository<Tlocation, int> locationsRepository) :
			this(options, blocksRepository, locationsRepository, new NetworkUtility(), new FileSystem(), new CsvReaderFactory())
		{ }

		internal GeoIPService(
			GeoIPServiceOptions options,
			IRepository<Tblock, UInt128> blocksRepository,
			IRepository<Tlocation, int> locationsRespository,
			INetworkUtility networkUtility,
			IFileSystem fileSystem,
			ICsvReaderFactory csvReaderFactory
		)
		{
			Options = options;
			_blocksRepository = blocksRepository;
			_locationsRepository = locationsRespository;
			_networkUtility = networkUtility;
			_fileSystem = fileSystem;
			_csvReaderFactory = csvReaderFactory;
		}

		private readonly IRepository<Tblock, UInt128> _blocksRepository;
		private readonly IRepository<Tlocation, int> _locationsRepository;
		private readonly INetworkUtility _networkUtility;
		private readonly IFileSystem _fileSystem;
		private readonly ICsvReaderFactory _csvReaderFactory;
		private const int _defaultCsvReaderChunkSize = 5000;

		public GeoIPServiceOptions Options { get; private set; }

		public Tblock? GetBlock(UInt128 ipNumber)
			=> _blocksRepository.GetSingle(ipNumber);

		public Tblock? GetBlock(string ipAddress)
			=> GetBlock(_networkUtility.GetIpNumberFromAddress(ipAddress));
		
		public Tlocation? GetLocation(int geoNameId)
			=> _locationsRepository.GetSingle(geoNameId);
		

		public void Load()
		{
			if(Options.CsvBlocksFileIPv4 is null && Options.CsvBlocksFileIPv6 is null)
				throw new InvalidOperationException("CsvBlocksFileIPv4 and CsvBlocksFileIPv6 option not defined");

			int chunkSize = Options.CsvReaderChunkSize ?? _defaultCsvReaderChunkSize;
			AddCsvFileToRepository(_blocksRepository, Options.CsvBlocksFileIPv4, chunkSize);
			AddCsvFileToRepository(_blocksRepository, Options.CsvBlocksFileIPv6, chunkSize);
			if (Options.CsvLocationsFile is not null)
				AddCsvFileToRepository(_locationsRepository, Options.CsvLocationsFile, chunkSize);
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

		private void AddCsvFileToRepository<T,Tid>(IRepository<T, Tid> repository, string? filePath, int chunkSize)
		{
			if (filePath is null) return;
			
			filePath = _fileSystem.PathGetFullPath(filePath);
			using StreamReader reader = _fileSystem.FileOpenText(filePath);
			ICsvReader<T> csvReader = _csvReaderFactory.Create<T>(reader);
			csvReader.Options.Header = csvReader.ReadLine();
			while(!csvReader.EndOfStream)
			{
				T[] items = csvReader.ReadObjects(chunkSize);
				if (items.Length == 0) continue;
				if (items[0] is ISubnet) UpdateSubnet(items.Select(x=> (ISubnet)x!));
				repository.Add(items);
			}
		}
	}
}
