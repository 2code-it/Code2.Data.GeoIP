using System;

namespace Code2.Data.GeoIP;
public class MaxmindMetaOptions
{
	public int UpdateIntervalInHours { get; set; }
	public string DownloadUrl { get; set; } = string.Empty;
	public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
	public string DefaultLanguage { get; set; } = string.Empty;
	public MaxmindEdititionFileInfo[] Files { get; set; } = Array.Empty<MaxmindEdititionFileInfo>();

}
