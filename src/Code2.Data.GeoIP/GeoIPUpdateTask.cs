using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.UpdateTasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Code2.Data.GeoIP
{
	public class GeoIPUpdateTask : HttpUpdateTask
	{
		public GeoIPUpdateTask() : this(new FileSystem())
		{ }
		internal GeoIPUpdateTask(IFileSystem fileSystem)
		{
			_fileSystem = fileSystem;
		}

		private readonly IFileSystem _fileSystem;

		public string OutputDirectory { get; set; } = "./";
		public string MaxmindDownloadUrl { get; set; } = string.Empty;
		public string MaxmindEdition { get; set; } = string.Empty;
		public string MaxmindLicenseKey { get; set; } = string.Empty;
		public bool HashCheckDownload { get; set; }

		private const string _http_header_last_modified = "last-modified";


		protected override IResult? OnBeforeRun()
		{
			Url = MaxmindDownloadUrl.Replace($"$({nameof(MaxmindEdition)})", MaxmindEdition).Replace($"$({nameof(MaxmindLicenseKey)})", MaxmindLicenseKey);
			string outputPath = PathGetFullPath(OutputDirectory);
			string fileName = $"{MaxmindEdition}.zip";
			FilePath = PathCombine(outputPath, fileName);
			DateTime? localModified = FileExists(FilePath) ? FileLastWriteTime(FilePath) : null;

			try
			{
				if (!DirectoryExists(outputPath)) DirectoryCreate(outputPath);
				Dictionary<string, string> headers = GetHeadersOnlyAsync(Url, RequestHeaders).Result;
				if (!headers.TryGetValue(_http_header_last_modified, out string? remoteModifiedString))
				{
					return Result.Cancel("Last modified header not found");
				}
				if (!DateTime.TryParseExact(remoteModifiedString, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime remoteModified))
				{
					return Result.Cancel($"Can't parse last modified date: {remoteModifiedString}");
				}
				if (localModified is not null && localModified >= remoteModified.ToLocalTime())
				{
					return Result.Cancel("Remote file isn't modified");
				}
			}
			catch (Exception ex)
			{
				return Result.Error($"{nameof(GeoIPUpdateTask)} failed, reason: {ex.Message}", ex);
			}
			return Result.Success();
		}

		protected override IResult? OnAfterRun()
		{
			try
			{
				byte[]? remoteSha256 = HashCheckDownload ? GetByteArrayAsync($"{Url}.sha256").Result : null;
				if (remoteSha256 is not null)
				{
					string remoteSha256String = Encoding.UTF8.GetString(remoteSha256);
					string localSha256String = _fileSystem.FileGetSha256Hex(remoteSha256String);
					if (remoteSha256String != localSha256String)
					{
						FileDelete(FilePath!);
						return Result.Cancel("Invalid file, hash mismatch");
					}
				}
				string outputDirFullPath = PathGetFullPath(OutputDirectory);
				_fileSystem.ZipArchiveExtracTo(FilePath!, outputDirFullPath);
			}
			catch (Exception ex)
			{
				return Result.Error($"{nameof(GeoIPUpdateTask)} failed, reason: {ex.Message}", ex);
			}

			return Result.Success();
		}
	}
}
