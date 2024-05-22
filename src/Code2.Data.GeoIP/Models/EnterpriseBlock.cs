
namespace Code2.Data.GeoIP.Models
{
	public class EnterpriseBlock : CityBlock
	{
		public int IspId { get; set; }
		public int IsLegitimateProxy { get; set; }
		public string? Domain { get; set; }
		public int CountryConfidence { get; set; }
		public int SubdivisionConfidence { get; set; }
		public int CityConfidence { get; set; }
		public int PostalConfidence { get; set; }
	}
}
