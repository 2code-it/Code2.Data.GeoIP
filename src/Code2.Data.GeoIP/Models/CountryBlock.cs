namespace Code2.Data.GeoIP.Models
{
	public class CountryBlock : BlockBase
	{
		public int RegisteredCountryGeonameId { get; set; }
		public int RepresentedCountryGeonameId { get; set; }
		public int IsAnonymousProxy { get; set; }
		public int IsSatelliteProvider { get; set; }
	}
}
