using System;
using System.Net;

namespace Code2.Data.GeoIP.Internals
{
	internal class NetworkUtility : INetworkUtility
	{
		public (UInt128 begin, UInt128 end) GetRangeFromCidr(string? cidr)
		{
			if (string.IsNullOrEmpty(cidr)) { return (0, 0); }
			string[] networkParts = cidr.Split('/');

			bool isMapped;
			UInt128 start = GetIpNumberFromAddress(networkParts[0], out isMapped);

			byte maskBits = Convert.ToByte(networkParts[1]);
			if (isMapped) maskBits += 96;

			UInt128 end = start + ((UInt128)Math.Pow(2, 128 - maskBits) - 1);
			return (start, end);
		}

		public UInt128 GetIpNumberFromAddress(string address, out bool mappedToIPv6)
		{
			byte[] addrBytes = GetIPv6AdressBytes(address, out mappedToIPv6);
			return GetIpNumberFromAddressBytes(addrBytes);
		}

		public UInt128 GetIpNumberFromAddress(string address)
			=> GetIpNumberFromAddress(address, out _);


		private byte[] GetIPv6AdressBytes(string address, out bool mappedToIPv6)
		{
			IPAddress ip = IPAddress.Parse(address);
			mappedToIPv6 = false;
			if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
			{
				ip = ip.MapToIPv6();
				mappedToIPv6 = true;
			}
			return ip.GetAddressBytes();
		}

		private UInt128 GetIpNumberFromAddressBytes(byte[] addressBytes)
		{
			UInt128 ipNumber = 0;
			for (int a = 0; a < 16; a++)
			{
				ipNumber |= (UInt128)addressBytes[a] << (15 - a) * 8;
			}
			return ipNumber;
		}
	}
}
