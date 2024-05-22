using System;

namespace Code2.Data.GeoIP
{
	public class GeoIPOptions
	{
		public string? CsvDataDirectory { get; set; }
		public string? CsvReaderErrorFile { get; set; }
		public string? CsvUpdaterErrorFile { get; set; }
		public string? MaxmindLicenseKey { get; set; }
		public string? MaxmindEdition { get; set; }
		public string? MaxmindDownloadUrl { get; set; }
		public bool? KeepDownloadedZipFile { get; set; }
		public bool? HashCheckDownload { get; set; }
		public int? UpdateIntervalInHours { get; set; }
		public bool? UpdateOnStart { get; set; }
		public bool? LoadOnStart { get; set; }
		public bool? EnableUpdates { get; set; }
		public string? LocationFileLanguage { get; set; }
		public Type? BlockType { get; set; }
		public Type? LocationType { get; set; }
		public Type? IspType { get; set; }
		public string? RepositoryTypeName { get; set; }
	}
}
