namespace Code2.Data.GeoIP
{
	public class CityLocation : CountryLocation
	{
		public string? Subdivision1IsoCode { get; set; }
		public string? Subdivision1Name { get; set; }
		public string? Subdivision2IsoCode { get; set; }
		public string? Subdivision2Name { get; set; }
		public string? CityName { get; set; }
		public string? MetroCode { get; set; }
		public string? TimeZone { get; set; }
	}
}
