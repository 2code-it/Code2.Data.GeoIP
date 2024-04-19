using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal class HttpClient : IHttpClient
	{
		internal HttpClient(System.Net.Http.HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		private readonly System.Net.Http.HttpClient _httpClient;

		public System.Net.Http.Headers.HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

		public async Task<string> GetStringAsync(string url)
			=> await _httpClient.GetStringAsync(url);

		public async Task<System.Net.Http.HttpResponseMessage> GetAsync(string url, System.Net.Http.HttpCompletionOption completionOption)
			=> await _httpClient.GetAsync(url, completionOption);

		public async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage requestMessage)
			=> await _httpClient.SendAsync(requestMessage);

		public async Task<Stream> GetStreamAsync(string url)
			=> await _httpClient.GetStreamAsync(url);

		public void Dispose()
			=> _httpClient.Dispose();

	}
}
