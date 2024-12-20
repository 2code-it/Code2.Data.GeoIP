using Code2.Tools.Csv.Repos;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Code2.Data.GeoIP;

public static class DependencyInjection
{
	public static IServiceCollection AddGeoIP(this IServiceCollection services, Action<GeoIPOptions>? config = null)
	{
		GeoIPOptions options = new();
		config?.Invoke(options);
		return services.AddGeoIP(options);
	}

	public static IServiceCollection AddGeoIP(this IServiceCollection services, GeoIPOptions options)
	{
		var networkUtility = new NetworkUtility();
		var optionsManager = new OptionsManager(networkUtility);
		optionsManager.Configure(options);
		CsvReposOptions csvReposOptions = optionsManager.GetCsvReposOptions();
		csvReposOptions.ServiceCollection = services;
		services.AddCsvRepos(csvReposOptions);
		services.AddSingleton<IOptionsManager>(optionsManager);
		services.AddSingleton<INetworkUtility>(networkUtility);
		return services;
	}

	public static IServiceProvider UseGeoIP(this IServiceProvider serviceProvider)
	{
		IOptionsManager optionsManager = serviceProvider.GetRequiredService<IOptionsManager>();
		var geoIPOptions = optionsManager.GetGeoIPOptions();
		bool updateOnStart = geoIPOptions.UpdateOnStart ?? false;
		bool loadOnstart = geoIPOptions.LoadOnStart ?? false;
		serviceProvider.UseCsvRepos(updateOnStart, loadOnstart);
		return serviceProvider;
	}
}
