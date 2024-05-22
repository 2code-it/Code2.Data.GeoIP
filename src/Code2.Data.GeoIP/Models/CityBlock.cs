namespace Code2.Data.GeoIP.Models
{
	public class CityBlock : CountryBlock
	{
		public string? PostalCode { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public int AccuracyRadius { get; set; }
	}
}
