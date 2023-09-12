namespace Code2.Data.GeoIP.Internals
{
	internal class FileSystem : IFileSystem
	{
		public string PathGetFullPath(string path)
			=> Path.GetFullPath(path, AppDomain.CurrentDomain.BaseDirectory);

		public StreamReader FileOpenText(string path)
			=> File.OpenText(path);
	}
}
