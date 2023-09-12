using Code2.Data.GeoIP.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIPTests
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
	}
}
