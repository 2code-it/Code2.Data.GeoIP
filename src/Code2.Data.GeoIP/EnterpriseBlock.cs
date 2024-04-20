using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class EnterpriseBlock: CityBlock
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
