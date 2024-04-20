using System;

namespace Code2.Data.GeoIP
{
	public class GeoIPServiceCountry : GeoIPService<CountryBlock, CountryLocation, IspBase>
	{
		public GeoIPServiceCountry() { }
		public GeoIPServiceCountry(GeoIPServiceOptions options) : base(options) { }
		public GeoIPServiceCountry(GeoIPServiceOptions options, IRepository<CountryBlock, UInt128> blocksRepository, IRepository<CountryLocation, int> locationsRepositiory)
			: base(options, blocksRepository, locationsRepositiory, new EmptyRepository<IspBase, int>()) { }
	}
}
