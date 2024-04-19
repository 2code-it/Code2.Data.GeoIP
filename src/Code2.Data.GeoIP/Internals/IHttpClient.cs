using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal interface IHttpClient: IDisposable
	{
		System.Net.Http.Headers.HttpRequestHeaders DefaultRequestHeaders { get; }
		Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption);
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage);
		Task<Stream> GetStreamAsync(string url);
		Task<string> GetStringAsync(string url);
	}
}