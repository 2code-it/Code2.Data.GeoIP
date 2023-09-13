# Code2.Data.GeoIP
Service for maxmind geoip csv data which defaults to storing the dat in memory. 
The csv files can be downloaded after registering at https://www.maxmind.com/.

## options
- CsvBlocksFileIPv4, path to csv file with ipv4 block data (optional if ipv6 is set)
- CsvBlocksFileIPv6, path to csv file with ipv6 block data (optional if ipv4 is set)
- CsvLocationsFile (optional), path to csv file with location data 
- CsvReaderChunkSize (optional), amount of lines to read and process, default=5000

## usage
As there are 2 types of csv files you can either use GeoIPServiceCity or GeoIPServiceCountry. 
If you're only interested in a few object attributes you can define and use your own objects as long as they are derived from LocationBase and BlockBase.
```
var options = new GeoIPServiceOptions()
{
	CsvBlocksFileIPv4 = "./data/GeoLite2-City-Blocks-IPv4.csv",
	CsvBlocksFileIPv6 = "./data/GeoLite2-City-Blocks-IPv6.csv",
	CsvLocationsFile = "./data/GeoLite2-City-Locations-en.csv"
};

public class MyBlock: BlockBase {public int AccuracyRadius {get;set;} }
public class MyLocation: LocationBase {public string? CityName {get;set;} }
var service = new GeoIPService<MyBlock, MyLocation>(options);
service.Load();
```

# remarks
The InMemoryBlocksRepository stores the data chunked with the chunksize equal to Options.CsvReaderChunkSize

# references
https://www.maxmind.com/