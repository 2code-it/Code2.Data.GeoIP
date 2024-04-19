namespace Code2.Data.GeoIP.Internals
{
	internal class HttpClientFactory : IHttpClientFactory
	{
		public IHttpClient Create()
			=> new HttpClient(new System.Net.Http.HttpClient());
	}
}
