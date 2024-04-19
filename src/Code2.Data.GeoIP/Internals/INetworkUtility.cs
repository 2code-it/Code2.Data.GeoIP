using System;
using System.IO;

namespace Code2.Data.GeoIP.Internals
{
	internal interface INetworkUtility
	{
		UInt128 GetIpNumberFromAddress(string address);
		UInt128 GetIpNumberFromAddress(string address, out bool mappedToIPv6);
		(UInt128 begin, UInt128 end) GetRangeFromCidr(string? cidr);
	}
}