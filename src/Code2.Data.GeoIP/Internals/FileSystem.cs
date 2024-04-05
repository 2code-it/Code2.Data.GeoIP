using System.IO.Compression;
using System.Security.Cryptography;

namespace Code2.Data.GeoIP.Internals
{
	internal class FileSystem : IFileSystem
	{
		public string PathGetFullPath(string path)
			=> Path.GetFullPath(path, AppDomain.CurrentDomain.BaseDirectory);

		public StreamReader FileOpenText(string path)
			=> File.OpenText(path);

		public Stream FileOpenRead(string path)
			=> File.OpenRead(path);

		public Stream FileOpen(string path)
			=> File.Open(path, FileMode.Open, FileAccess.ReadWrite);

		public Stream FileCreate(string path)
			=> File.Create(path);

		public void FileDelete(string path)
			=> File.Delete(path);

		public bool FileExists(string path)
			=> File.Exists(path);

		public string FileReadAllText(string path)
			=> File.ReadAllText(path);

		public string PathCombine(params string[] paths)
			=> Path.Combine(paths);

		public string[] DirectoryGetFiles(string path, string search)
			=> Directory.GetFiles(path, search);

		public void DirectoryCreate(string path)
			=> Directory.CreateDirectory(path);

		public string FileGetSha256Hex(Stream fileStream)
		{
			byte[] hashBytes = SHA256.HashData(fileStream);
			return Convert.ToHexString(hashBytes);
		}

		public void ZipArchiveExtractEntryTo(string zipFilePath, string fileFilter, string outputDirectory)
		{
			using Stream zipFileStream = FileOpenRead(zipFilePath);
			using ZipArchive zipArchive = new ZipArchive(zipFileStream);

			var entry = zipArchive.Entries.FirstOrDefault(x => x.Name.Contains(fileFilter));
			if (entry is null) return;

			string filePath = PathCombine(outputDirectory, entry.Name);
			entry.ExtractToFile(filePath, true);
		}

	}
}
