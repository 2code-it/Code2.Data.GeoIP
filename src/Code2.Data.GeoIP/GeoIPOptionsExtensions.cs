using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP;
public static class GeoIPOptionsExtensions
{
	public static GeoIPOptions UseTypes<Tblock, Tlocation>(this GeoIPOptions options)
		=> options.UseBlockType<Tblock>().UseLocationType<Tlocation>();

	public static GeoIPOptions UseTypes<Tblock, Tlocation, Tisp>(this GeoIPOptions options)
		=> options.UseBlockType<Tblock>().UseLocationType<Tlocation>().UseIspType<Tisp>();
	
	public static GeoIPOptions UseBlockType<T>(this GeoIPOptions options)
	{
		options.BlockTypeName = typeof(T).FullName;
		return options;
	}

	public static GeoIPOptions UseLocationType<T>(this GeoIPOptions options)
	{
		options.LocationTypeName = typeof(T).FullName;
		return options;
	}

	public static GeoIPOptions UseIspType<T>(this GeoIPOptions options)
	{
		options.IspTypeName = typeof(T).FullName;
		return options;
	}
}
