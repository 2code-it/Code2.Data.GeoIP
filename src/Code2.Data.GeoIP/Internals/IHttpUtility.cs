using System;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal interface IHttpUtility
	{
		Task DownloadFileToAsync(string url, string filePath, string? hashHex = null);
		Task<string> DownloadStringAsync(string url);
		Task<DateTime> GetLastModifiedHeaderAsync(string url);
	}
}