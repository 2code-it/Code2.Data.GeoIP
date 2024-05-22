using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using System;

namespace Code2.Data.GeoIP
{
	public class CsvUpdaterGeoIP : CsvUpdater
	{
		public CsvUpdaterGeoIP(GeoIPOptions geoOptions, CsvReposOptions csvReposOptions, ICsvLoader csvLoader) : this(geoOptions, csvReposOptions, csvLoader, new FileSystem())
		{ }
		internal CsvUpdaterGeoIP(GeoIPOptions geoOptions, CsvReposOptions csvReposOptions, ICsvLoader csvLoader, IFileSystem fileSystem) : base(csvReposOptions, csvLoader)
		{
			_fileSystem = fileSystem;
			if (geoOptions.CsvUpdaterErrorFile is not null) _csvUpdateErrorFilePath = _fileSystem.PathGetFullPath(geoOptions.CsvUpdaterErrorFile);
		}

		private readonly IFileSystem _fileSystem;
		private readonly string? _csvUpdateErrorFilePath;

		protected override void OnTaskError(ICsvUpdateTask updateTask, Exception exception, ref bool handled)
		{
			if (_csvUpdateErrorFilePath is null) return;
			handled = true;
			string logLine = $"{DateTime.Now:s}\t{updateTask.GetType().Name}\t{exception.Message}";
			_fileSystem.FileAppendAllLines(_csvUpdateErrorFilePath, new[] { logLine });
		}
	}
}
