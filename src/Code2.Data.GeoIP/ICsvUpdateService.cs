using System;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public interface ICsvUpdateService
	{
		string CsvDataDirectory { get; set; }
		string CsvDownloadUrl { get; set; }
		string[] CsvFileFilters { get; set; }
		string MaxmindEdition { get; set; }
		string MaxmindLicenseKey { get; set; }
		bool UseDownloadHashCheck { get; set; }
		bool IsUpdating { get; }

		event EventHandler<UnhandledExceptionEventArgs>? Error;
		event EventHandler? Update;

		DateTime GetFileLastWriteTime();
		void StartAutomaticUpdating();
		void StopAutomaticUpdating();
		Task UpdateFilesAsync();
	}
}