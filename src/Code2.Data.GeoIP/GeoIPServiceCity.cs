using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class GeoIPServiceCity : GeoIPService<CityBlock, CityLocation>
	{
		public GeoIPServiceCity() : this(new GeoIPServiceOptions()) { }
		public GeoIPServiceCity(GeoIPServiceOptions options) : base(options) { }
		public GeoIPServiceCity(GeoIPServiceOptions options, IRepository<CityBlock, UInt128> blocksRepository, IRepository<CityLocation, int> locationsRepositiory)
			: base(options, blocksRepository, locationsRepositiory) { }
	}
}
