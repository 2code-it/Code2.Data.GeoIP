# Code2.Data.GeoIP
Service for maxmind geoip csv data which defaults to storing the data in memory. 
The csv files can be downloaded after registering at https://www.maxmind.com/.

## options
- CsvDataDirectory, directory to store the csv files, default: "./data"
- CsvReaderErrorFile, path to csv error log file
- CsvUpdaterErrorFile, path to updater error log file
- LocationFileLanguage, locations file language, default: "en"
- MaxmindLicenseKey, maxmind license key
- MaxmindEdition, maxmind edition: GeoLite2-Country-CSV, GeoLite2-City-CSV, GeoIP2-Enterprise-CSV
- MaxmindDownloadUrl, alternate download url
- KeepDownloadedZipFile, indication to store the zipfile, default: false
- HashCheckDownload, indication to hash check the downloaded zipfile, default: false
- EnableUpdates, indication to enable periodic updating
- UpdateIntervalInHours, time in hours between updates
- UpdateOnStart, indication to run update on start, default: false
- LoadOnStart, indication to load csv files on start, default: false

## sample api
```
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Models;
using Code2.Tools.Csv.Repos;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var options = builder.Configuration.GetSection(nameof(GeoIPOptions)).Get<GeoIPOptions>();
builder.Services.AddGeoIP<CityBlock, CityLocation>(options: options);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet("/blocks/{ipAddress}", (IRepository<CityBlock> repo, string ipAddress) => repo.GetBlock(ipAddress));
app.MapGet("/locations/{geoNameId}", (IRepository<CityLocation> repo, int geoNameId) => repo.Get(x => x.GeoNameId == geoNameId).FirstOrDefault());

await app.Services.UseGeoIPAsync();

app.Run();

/* 
launchSettings.json
      "workingDirectory": "$(OutDir)",
      "launchBrowser": true,
      "launchUrl": "blocks/8.8.8.8"

appSettings.json
	"GeoIPOptions": {
		"MaxmindEdition": "GeoLite2-City-CSV",
		"MaxmindLicenseKey": "<maxmind_license_key>", //set license key here
		"CsvReaderErrorFile": "./csv_reader_error.log",
		"CsvUpdaterErrorFile": "./csv_updater_error.log",
		"EnableUpdates": true,
		"LoadOnStart": true,
		"UpdateOnStart": true
	}
*/
```

## sample console app with custom block and location classes
```
using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Models;
using Code2.Tools.Csv.Repos;
using Microsoft.Extensions.DependencyInjection;


IServiceCollection services = new ServiceCollection();
services.AddGeoIP<GeoIPBlock, GeoIPLocation>(options => {
	options.MaxmindEdition = "GeoLite2-City-CSV";
	options.MaxmindLicenseKey = "<your_license_key>"; //set license key here
	options.CsvReaderErrorFile = "./csv_reader_error.log";
	options.CsvUpdaterErrorFile = "./csv_updater_error.log";
	options.EnableUpdates = true;
	options.LoadOnStart = true;
	options.UpdateOnStart = true;
});

IServiceProvider serviceProvider = services.BuildServiceProvider();
await serviceProvider.UseGeoIPAsync();

IRepository<GeoIPBlock> blocksRepo = serviceProvider.GetRequiredService<IRepository<GeoIPBlock>>();
IRepository<GeoIPLocation> locationsRepo = serviceProvider.GetRequiredService<IRepository<GeoIPLocation>>();

Console.WriteLine("App ready, type in an ip address and hit enter..");
Console.CursorVisible = true;

while (true)
{
	string? ipAddress = Console.ReadLine();
	if (string.IsNullOrEmpty(ipAddress)) break;
	var block = blocksRepo.GetBlock(ipAddress);
	if(block is null)
	{
		Console.WriteLine("IP Address not found");
	}
	else
	{
		var location = locationsRepo.Get(x=>x.GeoNameId == block.GeoNameId).FirstOrDefault();
		Console.WriteLine($"latitude: {block.Latitude}, longitude: {block.Longitude}, country: {location?.CountryName}, city: {location?.CityName}");
	}
}

public class GeoIPLocation : LocationBase
{
	public string? CountryName { get; set; }
	public string? CityName { get; set; }
}

public class GeoIPBlock : BlockBase
{
	public double Latitude { get; set; }
	public double Longitude { get; set; }
}
```

## remarks
BlocksRepository stores the data chunked for quick access

## references
https://www.maxmind.com/
https://dev.maxmind.com/geoip/docs/databases/city-and-country