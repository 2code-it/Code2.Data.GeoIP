using System;

namespace Code2.Data.GeoIP
{
	public interface ISubnet
	{
		string? Network { get; set; }
		UInt128 BeginAddress { get; set; }
		UInt128 EndAddress { get; set; }
	}
}