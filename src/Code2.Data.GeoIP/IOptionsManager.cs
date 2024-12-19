using Code2.Tools.Csv.Repos;

namespace Code2.Data.GeoIP;
public interface IOptionsManager
{
	CsvReposOptions GetCsvReposOptions();
	GeoIPOptions GetGeoIPOptions();
	MaxmindMetaOptions GetMaxmindMetaOptions();
	void Reset();
	void Configure(GeoIPOptions options);
}