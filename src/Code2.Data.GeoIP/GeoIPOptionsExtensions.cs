using Code2.Data.GeoIP.Models;

namespace Code2.Data.GeoIP;
public static class GeoIPOptionsExtensions
{
	public static GeoIPOptions UseTypes<Tblock, Tlocation>(this GeoIPOptions options)
		where Tblock : BlockBase
		where Tlocation : LocationBase
		=> options.UseBlockType<Tblock>().UseLocationType<Tlocation>();

	public static GeoIPOptions UseTypes<Tblock, Tlocation, Tisp>(this GeoIPOptions options)
		where Tblock : BlockBase
		where Tlocation : LocationBase
		where Tisp : IspBase
		=> options.UseBlockType<Tblock>().UseLocationType<Tlocation>().UseIspType<Tisp>();

	public static GeoIPOptions UseBlockType<T>(this GeoIPOptions options)
		where T : BlockBase
	{
		options.BlockTypeName = typeof(T).FullName;
		return options;
	}

	public static GeoIPOptions UseLocationType<T>(this GeoIPOptions options)
		where T : LocationBase
	{
		options.LocationTypeName = typeof(T).FullName;
		return options;
	}

	public static GeoIPOptions UseIspType<T>(this GeoIPOptions options)
		where T : IspBase
	{
		options.IspTypeName = typeof(T).FullName;
		return options;
	}
}
