using Code2.Data.GeoIP.Internals;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class CsvUpdateService : ICsvUpdateService
	{
		public CsvUpdateService() : this(new FileSystem(), new HttpUtility(), new TaskUtility()) { }
		internal CsvUpdateService(IFileSystem fileSystem, IHttpUtility httpUtility, ITaskUtility taskUtility)
		{
			_fileSystem = fileSystem;
			_httpUtility = httpUtility;
			_taskUtility = taskUtility;
		}

		private readonly IFileSystem _fileSystem;
		private readonly IHttpUtility _httpUtility;
		private readonly ITaskUtility _taskUtility;
		private CancellationTokenSource? _cts;
		private static readonly SemaphoreSlim _semaphoreFileUpdate = new SemaphoreSlim(1);


		public event EventHandler? Update;
		public event EventHandler<UnhandledExceptionEventArgs>? Error;

		public string MaxmindLicenseKey { get; set; } = string.Empty;
		public string MaxmindEdition { get; set; } = string.Empty;
		public string CsvDownloadUrl { get; set; } = string.Empty;
		public string CsvDataDirectory { get; set; } = string.Empty;
		public bool UseDownloadHashCheck { get; set; }
		public string[] CsvFileFilters { get; set; } = Array.Empty<string>();
		public bool IsUpdating => _semaphoreFileUpdate.CurrentCount == 0;

		public async void StartAutomaticUpdating()
		{
			_cts = new CancellationTokenSource();
			while (!_cts.IsCancellationRequested)
			{
				try
				{
					DateTime remoteLastModfiedDate = (await _httpUtility.GetLastModifiedHeaderAsync(GetDownloadUrl())).Date;
					DateTime localLastModifiedDate = GetFileLastWriteTime().Date;
					DateTime currentTime = DateTime.Now;
					if (IsUpdating)
					{
						await _taskUtility.Delay(TimeSpan.FromHours(1), _cts.Token);
					}
					else if (remoteLastModfiedDate > localLastModifiedDate)
					{
						await UpdateFilesAsync();
						Update?.Invoke(this, EventArgs.Empty);
						await _taskUtility.Delay(TimeSpan.FromHours(72), _cts.Token);
					}
					else if (localLastModifiedDate.AddDays(3) < currentTime)
					{
						await _taskUtility.Delay(TimeSpan.FromHours(6), _cts.Token);
					}
					else
					{
						await _taskUtility.Delay(TimeSpan.FromHours(24), _cts.Token);
					}
				}
				catch (Exception ex)
				{
					OnError(ex);
					await _taskUtility.Delay(TimeSpan.FromHours(1), _cts.Token);
				}
			}
			_cts.Dispose();
		}

		public void StopAutomaticUpdating()
		{
			_cts?.Cancel();
		}

		public async Task UpdateFilesAsync()
		{
			await _semaphoreFileUpdate.WaitAsync();
			try
			{
				string dataDirectoryFullPath = _fileSystem.PathGetFullPath(CsvDataDirectory);
				EnsureDataDirectoryExists(dataDirectoryFullPath);

				string zipFilePath = _fileSystem.PathCombine(dataDirectoryFullPath, $"{MaxmindEdition}.zip");
				if (_fileSystem.FileExists(zipFilePath)) _fileSystem.FileDelete(zipFilePath);

				string downloadUrl = GetDownloadUrl();
				string? hashHex = UseDownloadHashCheck ? await _httpUtility.DownloadStringAsync($"{downloadUrl}.sha256") : null;

				await _httpUtility.DownloadFileToAsync(downloadUrl, zipFilePath, hashHex);
				foreach (string fileFilter in CsvFileFilters)
				{
					ExtractZipEntryIfExists(zipFilePath, fileFilter, dataDirectoryFullPath);
				}

				_fileSystem.FileDelete(zipFilePath);
			}
			catch (Exception ex)
			{
				OnError(ex);
			}
			finally
			{
				_semaphoreFileUpdate.Release();
			}
		}

		public DateTime GetFileLastWriteTime()
		{
			string dataDirectoryFullPath = _fileSystem.PathGetFullPath(CsvDataDirectory);
			EnsureDataDirectoryExists(dataDirectoryFullPath);

			string[] filePaths = _fileSystem.DirectoryGetFiles(dataDirectoryFullPath, "*.*");
			string? filePath = filePaths.FirstOrDefault(x => CsvFileFilters.Any(f => x.Contains(f)));
			return filePath is null ? DateTime.MinValue : _fileSystem.FileGetLastWriteTime(filePath);
		}

		private void OnError(Exception ex)
		{
			if (Error is null) throw ex;
			Error.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
		}

		private string GetDownloadUrl()
			=> CsvDownloadUrl.Replace($"$({nameof(MaxmindLicenseKey)})", MaxmindLicenseKey).Replace($"$({nameof(MaxmindEdition)})", MaxmindEdition);

		private void ExtractZipEntryIfExists(string zipFilePath, string? nameFilter, string destinationDirectory)
		{
			if (string.IsNullOrEmpty(nameFilter)) return;
			_fileSystem.ZipArchiveExtractEntryTo(zipFilePath, nameFilter, destinationDirectory);
		}

		private void EnsureDataDirectoryExists(string directoryPath)
		{
			if (_fileSystem.DirectoryExists(directoryPath)) return;
			_fileSystem.DirectoryCreate(directoryPath);
		}
	}
}
