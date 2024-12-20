namespace Code2.Data.GeoIP;

public class GeoIPOptions
{
	public string? DataDirectory { get; set; }
	public string? MaxmindLicenseKey { get; set; }
	public string? MaxmindEdition { get; set; }
	public string? MaxmindDownloadUrl { get; set; }
	public bool? HashCheckDownload { get; set; }
	public string? Language { get; set; }
	public string? BlockTypeName { get; set; }
	public string? LocationTypeName { get; set; }
	public string? IspTypeName { get; set; }
	public int? UpdateIntervalInHours { get; set; }
	public int? RetryIntervalInHours { get; set; }
	public bool? EnableUpdates { get; set; }
	public bool? UpdateOnStart { get; set; }
	public bool? LoadOnStart { get; set; }
	public bool? UseTransientRepositories { get; set; }
}
