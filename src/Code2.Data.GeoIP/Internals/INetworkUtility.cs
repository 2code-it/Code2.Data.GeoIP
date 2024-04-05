namespace Code2.Data.GeoIP.Internals
{
	internal interface INetworkUtility
	{
		string HttpGetString(string url);
		Stream HttpGetStream(string url);
		UInt128 GetIpNumberFromAddress(string address);
		UInt128 GetIpNumberFromAddress(string address, out bool mappedToIPv6);
		(UInt128 begin, UInt128 end) GetRangeFromCidr(string? cidr);
	}
}