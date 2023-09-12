namespace Code2.Data.GeoIP.Internals
{
	internal interface IFileSystem
	{
		StreamReader FileOpenText(string path);
		string PathGetFullPath(string path);
	}
}