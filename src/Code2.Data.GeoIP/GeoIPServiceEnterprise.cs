using System;
using System.Security.Cryptography;

namespace Code2.Data.GeoIP
{
	public class GeoIPServiceEnterprise : GeoIPService<EnterpriseBlock, EnterpriseLocation, EnterpriseIsp>
	{
		public GeoIPServiceEnterprise() { }
		public GeoIPServiceEnterprise(GeoIPServiceOptions options) : base(options) { }
		public GeoIPServiceEnterprise(GeoIPServiceOptions options, IRepository<EnterpriseBlock, UInt128> blocksRepository, IRepository<EnterpriseLocation, int> locationsRepositiory, IRepository<EnterpriseIsp, int> ispsRepositiory)
			: base(options, blocksRepository, locationsRepositiory, ispsRepositiory) { }
	}
}
