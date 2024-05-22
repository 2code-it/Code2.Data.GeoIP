using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class NetworkUtilityTests
	{
		[TestMethod]
		[DataRow("192.169.1.10/32", 32, true)]
		[DataRow("192.169.1.10/31", 31, true)]
		[DataRow("192.169.1.10/30", 30, true)]
		[DataRow("2001:218:c400::/126", 126, false)]
		[DataRow("2001:218:c400::/127", 127, false)]
		[DataRow("2001:218:c400::/96", 96, false)]
		[DataRow("2001:268:9036::/47", 47, false)]
		public void When_GetRangeFromCidr_WithAnyAddress_Expect_CalculatedRangeSize(string cidr, int maskBits, bool isIPv4)
		{
			NetworkUtility networkUtility = new NetworkUtility();

			var result = networkUtility.GetRangeFromCidr(cidr);

			if (isIPv4) maskBits += 96;
			UInt128 expectedRangeSize = (UInt128)Math.Pow(2, 128 - maskBits) - 1;
			UInt128 rangeSize = result.end - result.begin;

			Assert.AreEqual(expectedRangeSize, rangeSize);
		}

		[TestMethod]
		public void When_GetIpNumberFromAddress_WithIPv4Address_Expect_MappedToIPv6()
		{
			NetworkUtility networkUtility = new NetworkUtility();
			bool mapped;
			networkUtility.GetIpNumberFromAddress("129.17.12.1", out mapped);

			Assert.IsTrue(mapped);
		}

		[TestMethod]
		[DataRow("2001:268:9036::/47", true)]
		[DataRow("2001:268:9036::/132", false)]
		[DataRow(":268:9036::/10", false)]
		[DataRow("192.168.0.12", false)]
		[DataRow("192.168.0.12/31", true)]
		public void When_IsValidCidr_ValidAndInvalidValues_Expect_ValueSpecificResult(string cidr, bool expectedValue)
		{
			NetworkUtility networkUtility = new NetworkUtility();
			bool actual = networkUtility.IsValidCidr(cidr);

			Assert.AreEqual(expectedValue, actual);
		}

		[TestMethod]
		public void When_GetIpNumberFromAddress_WithIPv4Address_Expect_CorrespondingNumber()
		{
			NetworkUtility networkUtility = new NetworkUtility();
			string ipAddress = "129.17.12.1";
			IPAddress ip = IPAddress.Parse(ipAddress).MapToIPv6();
			UInt128 expected = GetNumberFromBytes(ip.GetAddressBytes());
			UInt128 actual = networkUtility.GetIpNumberFromAddress(ipAddress, out _);

			Assert.AreEqual(expected, actual);
		}

		private UInt128 GetNumberFromBytes(byte[] bytes)
		{
			UInt128 number = 0;
			for (int a = 0; a < 16; a++)
			{
				number |= (UInt128)bytes[a] << (15 - a) * 8;
			}
			return number;
		}
	}
}
