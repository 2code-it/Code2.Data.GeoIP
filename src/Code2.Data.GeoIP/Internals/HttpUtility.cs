using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal class HttpUtility : IHttpUtility
	{
		internal HttpUtility() : this(new HttpClientFactory(), new FileSystem()) { }
		internal HttpUtility(IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
		{
			_httpClientFactory = httpClientFactory;
			_fileSystem = fileSystem;
			
		}

		private readonly IFileSystem _fileSystem;
		private readonly IHttpClientFactory _httpClientFactory;
		private const string _last_modified_header_key = "last-modified";
		private const string _user_agent_header_key = "user-agent";
		private const string _user_agent_header_value = "dotnet/HttpClient";

		public async Task<DateTime> GetLastModifiedHeaderAsync(string url)
		{
			using IHttpClient client = CreateHttpClient();

			var requestMessage = CreateRequestMessage(System.Net.Http.HttpMethod.Head, url);
			System.Net.Http.HttpResponseMessage response = await client.SendAsync(requestMessage);
			if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Response error {response.StatusCode}, {response.ReasonPhrase}");
			string? modifiedString = response.Content.Headers.Where(x => x.Key.Equals(_last_modified_header_key, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value.FirstOrDefault()).FirstOrDefault();
			if (modifiedString is null) throw new InvalidOperationException($"Response did not contain header {_last_modified_header_key}");
			return DateTime.ParseExact(modifiedString, "r", CultureInfo.InvariantCulture).ToLocalTime();
		}

		public Task<string> DownloadStringAsync(string url)
		{
			using IHttpClient client = CreateHttpClient();
			return client.GetStringAsync(url);
		}

		public async Task DownloadFileToAsync(string url, string filePath, string? hashHex = null)
		{
			using IHttpClient httpClient = CreateHttpClient();
			using Stream httpStream = await httpClient.GetStreamAsync(url);
			using Stream fileStream = _fileSystem.FileCreate(filePath);

			httpStream.CopyTo(fileStream);
			await fileStream.FlushAsync();

			fileStream.Position = 0;
			string? streamHex = hashHex is not null ? _fileSystem.FileGetSha256Hex(fileStream) : null;

			fileStream.Close();
			httpStream.Close();

			if (hashHex != streamHex)
			{
				_fileSystem.FileDelete(filePath);
				throw new InvalidOperationException("File hash mismatch");
			}
		}

		private IHttpClient CreateHttpClient()
		{
			IHttpClient httpClient = _httpClientFactory.Create();
			httpClient.DefaultRequestHeaders.Add(_user_agent_header_key, _user_agent_header_value);
			return httpClient;
		}

		private System.Net.Http.HttpRequestMessage CreateRequestMessage(System.Net.Http.HttpMethod method, string url)
		{
			System.Net.Http.HttpRequestMessage message = new System.Net.Http.HttpRequestMessage();
			message.Method = method;
			message.RequestUri = new Uri(url);
			return message;
		}
	}
}
