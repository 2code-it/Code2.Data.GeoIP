namespace Code2.Data.GeoIP
{
	public class GeoIPServiceOptions
	{
		public string? CsvBlocksFileIPv4 { get; set; }
		public string? CsvBlocksFileIPv6 { get; set; }
		public string? CsvLocationsFile { get; set; }
		public int? CsvReaderChunkSize { get; set; }
	}
}
