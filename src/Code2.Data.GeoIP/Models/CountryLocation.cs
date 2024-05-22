namespace Code2.Data.GeoIP.Models
{
	public class CountryLocation : LocationBase
	{
		public string? LocaleCode { get; set; }
		public string? ContinentCode { get; set; }
		public string? ContinentName { get; set; }
		public string? CountryIsoCode { get; set; }
		public string? CountryName { get; set; }
		public int IsInEuropeanUnion { get; set; }
	}
}
