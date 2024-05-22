using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class CsvUpdateTaskGeoIP : CsvUpdateTaskBase
	{
		public CsvUpdateTaskGeoIP(CsvReposOptions csvReposOptions) : this(csvReposOptions, new FileSystem(), new HttpUtility())
		{ }
		internal CsvUpdateTaskGeoIP(CsvReposOptions csvReposOptions, IFileSystem fileSystem, IHttpUtility httpUtility)
		{
			_csvReposOptions = csvReposOptions;
			_fileSystem = fileSystem;
			_httpUtility = httpUtility;
		}

		private readonly CsvReposOptions _csvReposOptions;
		private readonly IFileSystem _fileSystem;
		private readonly IHttpUtility _httpUtility;

		public string MaxmindDownloadUrl { get; set; } = string.Empty;
		public string MaxmindEdition { get; set; } = string.Empty;
		public string MaxmindLicenseKey { get; set; } = string.Empty;
		public bool HashCheckDownload { get; set; }
		public bool KeepDownloadedZipFile { get; set; }

		public override async Task<bool> CanRunAsync()
		{
			string dataDirectoryPath = GetDataDirectoryPath();
			EnsureDirectoryExists(dataDirectoryPath);
			string? firstCsvFile = _fileSystem.DirectoryGetFiles(dataDirectoryPath, "*.csv").FirstOrDefault();
			DateTime lastModifiedLocal = firstCsvFile is null ? DateTime.MinValue : _fileSystem.FileGetLastWriteTime(firstCsvFile).AddHours(1);
			DateTime lastModifiedRemote;
			try
			{
				lastModifiedRemote = await _httpUtility.GetLastModifiedHeaderAsync(GetDownloadUrl());
			}
			catch
			{
				return false;
			}
			return lastModifiedRemote > lastModifiedLocal;
		}

		public override async Task RunAsync()
		{
			string dataDirectoryPath = GetDataDirectoryPath();
			EnsureDirectoryExists(dataDirectoryPath);
			string zipFilePath = _fileSystem.PathCombine(dataDirectoryPath, $"{MaxmindEdition}.zip");
			if (_fileSystem.FileExists(zipFilePath)) _fileSystem.FileDelete(zipFilePath);
			string url = GetDownloadUrl();

			string? hash = HashCheckDownload ? await _httpUtility.DownloadStringAsync($"{url}.sha256") : null;
			await _httpUtility.DownloadFileToAsync(url, zipFilePath, hash);

			foreach (CsvFileInfo fileInfo in _csvReposOptions.Files)
			{
				ExtractZipEntryIfExists(zipFilePath, fileInfo.NameFilter, dataDirectoryPath);
			}

			if (!KeepDownloadedZipFile) _fileSystem.FileDelete(zipFilePath);
		}

		private void EnsureDirectoryExists(string path)
		{
			if (_fileSystem.DirectoryExists(path)) return;
			_fileSystem.DirectoryCreate(path);
		}

		private string GetDataDirectoryPath()
			=> _fileSystem.PathGetFullPath(_csvReposOptions.CsvDataDirectory);

		private string GetDownloadUrl()
			=> MaxmindDownloadUrl.Replace($"$({nameof(MaxmindEdition)})", MaxmindEdition).Replace($"$({nameof(MaxmindLicenseKey)})", MaxmindLicenseKey);

		private void ExtractZipEntryIfExists(string zipFilePath, string? nameFilter, string destinationDirectory)
		{
			if (string.IsNullOrEmpty(nameFilter)) return;
			_fileSystem.ZipArchiveExtractEntryTo(zipFilePath, nameFilter, destinationDirectory);
		}
	}
}
