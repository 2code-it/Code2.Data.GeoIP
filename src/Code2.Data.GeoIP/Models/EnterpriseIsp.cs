
namespace Code2.Data.GeoIP.Models
{
	public class EnterpriseIsp : IspBase
	{
		public string? Isp { get; set; }
		public string? Organization { get; set; }
		public int AutonomousSystemNumber { get; set; }
		public string? AutonomousSystemOrganization { get; set; }
		public string? ConnectionType { get; set; }
		public string? UserType { get; set; }
		public string? MobileCountryCode { get; set; }
		public string? MobileNetworkCode { get; set; }
	}
}
