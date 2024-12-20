using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace Code2.Data.GeoIP.Internals;

internal class FileSystem : IFileSystem
{
	public string PathGetFullPath(string path)
		=> Path.GetFullPath(path);

	public string PathCombine(params string[] paths)
		=> Path.Combine(paths);

	public bool FileExists(string path)
		=> File.Exists(path);

	public Stream FileOpenRead(string path)
		=> File.OpenRead(path);

	public Stream? GetManifestResourceStream(string fileName)
	{
		string? name = typeof(FileSystem).Assembly.GetManifestResourceNames().Where(x => x.EndsWith(fileName)).FirstOrDefault();
		if (name is null) return null;
		return typeof(FileSystem).Assembly.GetManifestResourceStream(name);
	}

	public string FileGetSha256Hex(string filePath)
	{
		using var fileStream = File.OpenRead(filePath);
		byte[] hashBytes = SHA256.HashData(fileStream);
		fileStream.Close();
		return Convert.ToHexString(hashBytes);
	}

	public void ZipArchiveExtracTo(string zipFilePath, string outputDirectory)
	{
		using Stream zipFileStream = File.OpenRead(zipFilePath);
		using ZipArchive zipArchive = new ZipArchive(zipFileStream);

		foreach (var entry in zipArchive.Entries)
		{
			string filePath = Path.Combine(outputDirectory, entry.Name);
			entry.ExtractToFile(filePath, true);
		}
	}
}
