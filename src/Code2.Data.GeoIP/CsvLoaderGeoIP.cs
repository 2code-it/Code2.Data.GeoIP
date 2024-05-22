using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using System;
using System.Linq;

namespace Code2.Data.GeoIP
{
	public class CsvLoaderGeoIP : CsvLoader
	{
		public CsvLoaderGeoIP(IServiceProvider serviceProvider, GeoIPOptions options) : this(serviceProvider, options, new NetworkUtility(), new FileSystem()) { }
		internal CsvLoaderGeoIP(IServiceProvider serviceProvider, GeoIPOptions options, INetworkUtility networkUtility, IFileSystem fileSystem) : base(serviceProvider)
		{
			_networkUtility = networkUtility;
			_fileSystem = fileSystem;
			_csvReaderErrorFilePath = options.CsvReaderErrorFile is null ? null : _fileSystem.PathGetFullPath(options.CsvReaderErrorFile);
		}

		private readonly INetworkUtility _networkUtility;
		private readonly IFileSystem _fileSystem;
		private readonly string? _csvReaderErrorFilePath;

		public override void OnCsvReaderError(Exception exception, ref bool handled)
		{
			if (_csvReaderErrorFilePath is null) return;
			handled = true;
			string logLine = $"{DateTime.Now:s}\t{exception.Message}";
			_fileSystem.FileAppendAllLines(_csvReaderErrorFilePath, new[] { logLine });
		}

		public override void OnLoadData<T>(T[] items)
		{
			if (!typeof(T).IsAssignableTo(typeof(ISubnet))) return;
			var subnets = items.Cast<ISubnet>();
			foreach (ISubnet subnet in subnets)
			{
				var (begin, end) = _networkUtility.GetRangeFromCidr(subnet.Network);
				subnet.BeginAddress = begin;
				subnet.EndAddress = end;
			}
		}
	}
}
