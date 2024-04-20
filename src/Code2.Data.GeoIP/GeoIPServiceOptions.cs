namespace Code2.Data.GeoIP
{
	public class GeoIPServiceOptions
	{
		public string? MaxmindLicenseKey { get; set; }
		public string? MaxmindEdition { get; set; }
		public string? CsvDownloadUrl { get; set; }
		public bool UseDownloadHashCheck { get; set; }
		public string? CsvDataDirectory { get; set; }
		public string? CsvBlocksIPv4FileFilter { get; set; }
		public string? CsvBlocksIPv6FileFilter { get; set; }
		public string? CsvLocationsFileFilter { get; set; }
		public string? CsvIspFileFilter { get; set; }
		public int CsvReaderChunkSize { get; set; }
		public string? CsvReaderErrorLogFile { get; set; }
		public bool AutoUpdate { get; set; }
		public bool AutoLoad { get; set; }
	}
}
