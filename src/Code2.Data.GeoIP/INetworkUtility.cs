using System;

namespace Code2.Data.GeoIP
{
	public interface INetworkUtility
	{
		UInt128 GetIpNumberFromAddress(string address);
		UInt128 GetIpNumberFromAddress(string address, out bool mappedToIPv6);
		(UInt128 begin, UInt128 end) GetRangeFromCidr(string? cidr);
		bool IsValidIPAddress(string address);
		bool IsValidCidr(string cidr);
	}
}