using System.IO;

namespace Code2.Data.GeoIP.Internals;
internal interface IFileSystem
{
	string PathGetFullPath(string path);
	string PathCombine(params string[] paths);
	bool FileExists(string path);
	Stream FileOpenRead(string path);
	Stream? GetManifestResourceStream(string name);
	string FileGetSha256Hex(string filePath);
	void ZipArchiveExtracTo(string zipFilePath, string outputDirectory);
}