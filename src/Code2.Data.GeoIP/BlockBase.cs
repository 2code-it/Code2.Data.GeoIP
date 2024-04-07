using System;

namespace Code2.Data.GeoIP
{
	public class BlockBase : ISubnet
	{
		public int GeoNameId { get; set; }
		public string? Network { get; set; }
		public UInt128 BeginAddress { get; set; }
		public UInt128 EndAddress { get; set; }
	}
}
