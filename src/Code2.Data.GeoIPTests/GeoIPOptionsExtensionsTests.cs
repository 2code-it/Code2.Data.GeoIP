using Code2.Data.GeoIP;
using Code2.Data.GeoIPTests.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Code2.Data.GeoIPTests;

[TestClass]
public class GeoIPOptionsExtensionsTests
{
	[TestMethod]
	public void UseBlockType_When_Used_Expect_BlockTypeNameSet()
	{
		GeoIPOptions options = new();

		options.UseBlockType<TestBlock>();

		Assert.AreEqual(typeof(TestBlock).FullName, options.BlockTypeName);
	}

	[TestMethod]
	public void UseLocationType_When_Used_Expect_LocationTypeNameSet()
	{
		GeoIPOptions options = new();

		options.UseLocationType<TestLocation>();

		Assert.AreEqual(typeof(TestLocation).FullName, options.LocationTypeName);
	}

	[TestMethod]
	public void UseIspType_When_Used_Expect_IspTypeNameSet()
	{
		GeoIPOptions options = new();

		options.UseIspType<TestIsp>();

		Assert.AreEqual(typeof(TestIsp).FullName, options.IspTypeName);
	}

	[TestMethod]
	public void UseTypes_When_Used_Expect_TypeNamesSet()
	{
		GeoIPOptions options = new();

		options.UseTypes<TestBlock, TestLocation, TestIsp>();

		Assert.AreEqual(typeof(TestBlock).FullName, options.BlockTypeName);
		Assert.AreEqual(typeof(TestLocation).FullName, options.LocationTypeName);
		Assert.AreEqual(typeof(TestIsp).FullName, options.IspTypeName);
	}
}
