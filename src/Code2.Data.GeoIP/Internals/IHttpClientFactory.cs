namespace Code2.Data.GeoIP.Internals
{
	internal interface IHttpClientFactory
	{
		IHttpClient Create();
	}
}