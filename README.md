# Code2.Data.GeoIP
Service for maxmind geoip csv data which defaults to storing the data in memory. 
The csv files can be downloaded after registering at https://www.maxmind.com/.

## options
- MaxmindLicenceKey, can be obtained after registering a free maxmind account, default: ""
- MaxmindEdition, edition specifier GeoLite2-Country-CSV, GeoLite2-City-CSV, ..etc, default: "GeoLite2-City-CSV"
- CsvDownloadUrl, csv download url template with placeholders for \$(MaxmindEdition) and \$(MaxmindLicenceKey), default: <maxmind-csv-download-url>
- CsvDataDirectory, directory to store the csv files, default: "./data"
- CsvBlocksIPv4FileFilter, csv ipv4 blocks *file filter, default: "Blocks-IPv4.csv"
- CsvBlocksIPv6FileFilter, csv ipv6 blocks *file filter, default: "Blocks-IPv6.csv"
- CsvLocationsFileFilter, csv locations *file filter, default: "Locations-en.csv"
- CsvReaderChunkSize, amount of lines to read and process, default=5000
- CsvReaderErrorLogFile, log file for csv read errors, defualt: "./data/csv_error.txt"

*file filter is used search for a specific file and can be set to null to prevent the file from loading

## usage
As there are 2 types of csv files you can either use GeoIPServiceCity or GeoIPServiceCountry. 

```
using System;
using Code2.Data.GeoIP;


var geoIPService = new GeoIPServiceCity();
// optional when manual updating csv files
geoIPService.Options.MaxmindEdition = "GeoLite2-City-CSV"; // or GeoLite2-Country-CSV for GeoIPServiceCountry
geoIPService.Options.MaxmindLicenseKey = "<maxmind_licence_key>";

if((DateTime.Now - geoIPService.GetLastFileWriteTime()).TotalDays > 10)
{
	geoIPService.UpdateFiles();
}
geoIPService.Load();

Console.Write("lookup address:");
while (true)
{
	string? line = Console.ReadLine();
	if (string.IsNullOrEmpty(line)) break;
	CityBlock? cityBlock = geoIPService.GetBlock(line);
	CityLocation? cityLocation = cityBlock is null? null: geoIPService.GetLocation(cityBlock.GeoNameId);
	string result = cityLocation is null ? "not found" : $"{cityLocation.CountryName} {cityLocation.CityName}";
	Console.WriteLine(result);
}
```

If you're only interested in a few object attributes you can define and use your own objects as long as they are derived from LocationBase and BlockBase.
```

public class MyBlock: BlockBase {public int AccuracyRadius {get;set;} }
public class MyLocation: LocationBase {public string? CityName {get;set;} }
var service = new GeoIPService<MyBlock, MyLocation>(options);
service.Load();
```

# remarks
The InMemoryBlocksRepository stores the data chunked with the chunksize equal to Options.CsvReaderChunkSize

# references
https://www.maxmind.com/