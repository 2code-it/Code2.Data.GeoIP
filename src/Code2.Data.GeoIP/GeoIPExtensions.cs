using Code2.Data.GeoIP.Models;
using Code2.Tools.Csv.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public static class GeoIPExtensions
	{
		private static readonly INetworkUtility _networkUtility = new NetworkUtility();

		public static void AddGeoIP(this IServiceCollection services, Action<GeoIPOptions>? config)
		{
			GeoIPOptions geoOptions = new GeoIPOptions();
			config?.Invoke(geoOptions);
			AddGeoIP(services, geoOptions);
		}

		public static void AddGeoIP(this IServiceCollection services, GeoIPOptions geoOptions)
		{
			CsvReposOptions repoOptions = GetDefaultCsvReposOptions();

			MapOptions(geoOptions, repoOptions);

			services.TryAddSingleton<ICsvLoader, CsvLoaderGeoIP>();
			services.TryAddSingleton<ICsvUpdater, CsvUpdaterGeoIP>();
			services.TryAddSingleton<INetworkUtility, NetworkUtility>();
			services.TryAddSingleton(geoOptions);

			services.AddCsvRepos(repoOptions);
		}

		public static void AddGeoIP<Tblock>(this IServiceCollection services, Action<GeoIPOptions>? config = null, GeoIPOptions? options = null)
			where Tblock : ISubnet
		{
			void newConfig(GeoIPOptions optionsConfig)
			{
				MapOptions(options, optionsConfig);
				config?.Invoke(optionsConfig);
				optionsConfig.BlockType = typeof(Tblock);
			};
			AddGeoIP(services, newConfig);
		}

		public static void AddGeoIP<Tblock, Tlocation>(this IServiceCollection services, Action<GeoIPOptions>? config = null, GeoIPOptions? options = null)
			where Tblock : ISubnet
		{
			void newConfig(GeoIPOptions optionsConfig)
			{
				MapOptions(options, optionsConfig);
				config?.Invoke(optionsConfig);
				optionsConfig.BlockType = typeof(Tblock);
				optionsConfig.LocationType = typeof(Tlocation);
			};
			AddGeoIP(services, newConfig);
		}

		public static void AddGeoIP<Tblock, Tlocation, Tisp>(this IServiceCollection services, Action<GeoIPOptions>? config = null, GeoIPOptions? options = null)
			where Tblock : ISubnet
		{
			void newConfig(GeoIPOptions optionsConfig)
			{
				MapOptions(options, optionsConfig);
				config?.Invoke(optionsConfig);
				optionsConfig.BlockType = typeof(Tblock);
				optionsConfig.LocationType = typeof(Tlocation);
				optionsConfig.IspType = typeof(Tisp);
			}
			AddGeoIP(services, newConfig);
		}

		public static async Task UseGeoIPAsync(this IServiceProvider serviceProvider, bool? loadOnStart = null, bool? updateOnStart = null)
		{
			await serviceProvider.UseCsvReposAsync(loadOnStart, updateOnStart);
		}

		public static void ConfigureGeoIP(this IServiceProvider serviceProvider, GeoIPOptions options)
		{
			var repoOptions = serviceProvider.GetRequiredService<CsvReposOptions>();
			MapOptions(options, repoOptions);
		}

		public static Tblock? GetBlock<Tblock>(this IRepository<Tblock> repository, string ipAddress)
			where Tblock : ISubnet
		{
			UInt128 ipNumber = _networkUtility.GetIpNumberFromAddress(ipAddress);
			BlocksRepository<Tblock> blocksRepository = (BlocksRepository<Tblock>)repository;
			return blocksRepository.GetBlock(ipNumber);
		}

		private static void MapOptions(GeoIPOptions geoOptions, CsvReposOptions repoOptions)
		{
			UpdateOrRemoveFileInfo(repoOptions, nameof(BlockBase), geoOptions.BlockType, geoOptions.RepositoryTypeName);
			UpdateOrRemoveFileInfo(repoOptions, nameof(LocationBase), geoOptions.LocationType, geoOptions.RepositoryTypeName);
			UpdateOrRemoveFileInfo(repoOptions, nameof(IspBase), geoOptions.IspType, geoOptions.RepositoryTypeName);

			if (geoOptions.LocationFileLanguage is not null)
			{
				CsvFileInfo locationFileInfo = repoOptions.Files.FirstOrDefault(x => x.NameFilter.StartsWith("Locations")) ?? throw new InvalidOperationException("Can't set locations language, locations not configured");
				locationFileInfo.NameFilter = $"Locations-{geoOptions.LocationFileLanguage}.csv";
			}

			if (geoOptions.EnableUpdates ?? false)
			{
				if (geoOptions.MaxmindDownloadUrl is not null) repoOptions.UpdateTasks[0].TaskProperties[nameof(GeoIPOptions.MaxmindDownloadUrl)] = geoOptions.MaxmindDownloadUrl;
				if (geoOptions.MaxmindLicenseKey is not null) repoOptions.UpdateTasks[0].TaskProperties[nameof(GeoIPOptions.MaxmindLicenseKey)] = geoOptions.MaxmindLicenseKey;
				if (geoOptions.MaxmindEdition is not null) repoOptions.UpdateTasks[0].TaskProperties[nameof(GeoIPOptions.MaxmindEdition)] = geoOptions.MaxmindEdition;
				if (geoOptions.KeepDownloadedZipFile.HasValue) repoOptions.UpdateTasks[0].TaskProperties[nameof(GeoIPOptions.KeepDownloadedZipFile)] = geoOptions.KeepDownloadedZipFile.Value.ToString();
				if (geoOptions.HashCheckDownload.HasValue) repoOptions.UpdateTasks[0].TaskProperties[nameof(GeoIPOptions.HashCheckDownload)] = geoOptions.HashCheckDownload.Value.ToString();
				if (geoOptions.UpdateIntervalInHours.HasValue) repoOptions.UpdateTasks[0].IntervalInHours = geoOptions.UpdateIntervalInHours.Value;
			}
			else
			{
				repoOptions.UpdateTasks.Clear();
			}
			if (geoOptions.UpdateOnStart.HasValue) repoOptions.UpdateOnStart = geoOptions.UpdateOnStart.Value;
			if (geoOptions.LoadOnStart.HasValue) repoOptions.LoadOnStart = geoOptions.LoadOnStart.Value;
		}

		private static CsvReposOptions GetDefaultCsvReposOptions()
		{
			Type currentType = typeof(GeoIPExtensions);
			using Stream stream = currentType.Assembly.GetManifestResourceStream(currentType, $"{nameof(CsvReposOptions)}.json")!;
			return JsonSerializer.Deserialize<CsvReposOptions>(stream)!;
		}

		private static void UpdateOrRemoveFileInfo(CsvReposOptions options, string typeName, Type? targetType, string? repositoryTypeName)
		{
			var fileInfos = options.Files.Where(x => x.TargetTypeName == typeName).ToArray();
			foreach (var fileInfo in fileInfos)
			{
				if (targetType is null)
				{
					options.Files.Remove(fileInfo);
				}
				else
				{
					fileInfo.TargetTypeName = targetType.Name;
					if (repositoryTypeName is not null) fileInfo.RepositoryTypeName = repositoryTypeName;
				}
			}
		}

		private static void MapOptions(GeoIPOptions? source, GeoIPOptions target)
		{
			if (source is null) return;
			var properties = typeof(GeoIPOptions).GetProperties().Where(x => x.CanWrite);
			foreach (var property in properties)
			{
				property.SetValue(target, property.GetValue(source));
			}
		}
	}
}
