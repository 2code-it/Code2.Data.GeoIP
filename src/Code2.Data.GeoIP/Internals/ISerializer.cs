namespace Code2.Data.GeoIP.Internals;

internal interface ISerializer
{
	T DeserializerFromFileOrResource<T>();
}