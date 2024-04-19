using System;

namespace Code2.Data.GeoIP
{
	public class GeoIPServiceCity : GeoIPService<CityBlock, CityLocation>
	{
		public GeoIPServiceCity() { }
		public GeoIPServiceCity(GeoIPServiceOptions options) : base(options) { }
		public GeoIPServiceCity(GeoIPServiceOptions options, IRepository<CityBlock, UInt128> blocksRepository, IRepository<CityLocation, int> locationsRepositiory)
			: base(options, blocksRepository, locationsRepositiory) { }
	}
}
