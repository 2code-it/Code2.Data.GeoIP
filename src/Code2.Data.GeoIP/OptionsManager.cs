using Code2.Data.GeoIP.Internals;
using Code2.Data.GeoIP.Models;
using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP;
public class OptionsManager : IOptionsManager
{
	public OptionsManager(INetworkUtility networkUtility) : this(networkUtility, new Serializer(), new FileSystem())
	{ }

	internal OptionsManager(INetworkUtility networkUtility, ISerializer serializer, IFileSystem fileSystem)
	{
		_networkUtility = networkUtility;
		_serializer = serializer;
		_fileSystem = fileSystem;
		_maxmindOptions = serializer.DeserializerFromFileOrResource<MaxmindMetaOptions>();
		_csvReposOptions = serializer.DeserializerFromFileOrResource<CsvReposOptions>();
		_geoIPOptions = new();
		Reset();
	}

	private readonly INetworkUtility _networkUtility;
	private readonly ISerializer _serializer;
	private readonly IFileSystem _fileSystem;
	private readonly MaxmindMetaOptions _maxmindOptions;
	private GeoIPOptions _geoIPOptions;
	private CsvReposOptions _csvReposOptions;
	private Dictionary<string, string?> _baseTypeNameMappings = new();

	private const string _block_base_type_name = nameof(BlockBase);
	private const string _location_base_type_name = nameof(LocationBase);
	private const string _isp_base_type_name = nameof(IspBase);

	public GeoIPOptions GetGeoIPOptions()
		=> _geoIPOptions;

	public MaxmindMetaOptions GetMaxmindMetaOptions()
		=> _maxmindOptions;

	public CsvReposOptions GetCsvReposOptions()
		=> _csvReposOptions;

	public void Reset()
	{
		_geoIPOptions = _serializer.DeserializerFromFileOrResource<GeoIPOptions>();
		_geoIPOptions.MaxmindDownloadUrl ??= _maxmindOptions.DownloadUrl;
		_geoIPOptions.UpdateIntervalInHours ??= _maxmindOptions.UpdateIntervalInHours;
		_geoIPOptions.RetryIntervalInHours ??= _maxmindOptions.RetryIntervalInHours;
		_geoIPOptions.Language ??= _maxmindOptions.DefaultLanguage;
	}

	public void Configure(GeoIPOptions options)
	{
		Update(options);
		_csvReposOptions = CreateCsvReposOptions(_geoIPOptions);
	}


	private void Update(GeoIPOptions options)
	{
		string[] editions = _maxmindOptions.Files.Select(x => x.Edition).Distinct().ToArray();

		if (options.DataDirectory is not null) _geoIPOptions.DataDirectory = options.DataDirectory;
		if (options.MaxmindLicenseKey is not null) _geoIPOptions.MaxmindLicenseKey = options.MaxmindLicenseKey;
		if (options.MaxmindEdition is not null)
		{
			if (!editions.Contains(options.MaxmindEdition)) throw new InvalidOperationException($"Maxmind edition '{options.MaxmindEdition}' not supported");
			_geoIPOptions.MaxmindEdition = options.MaxmindEdition;
		}
		if (options.MaxmindDownloadUrl is not null) _geoIPOptions.MaxmindDownloadUrl = options.MaxmindDownloadUrl;
		if (options.HashCheckDownload is not null) _geoIPOptions.HashCheckDownload = options.HashCheckDownload;
		if (options.Language is not null)
		{
			if (!_maxmindOptions.SupportedLanguages.Contains(options.Language)) throw new InvalidOperationException($"Language '{options.Language}' not supported");
			_geoIPOptions.Language = options.Language;
		}
		if (options.BlockTypeName is not null) _geoIPOptions.BlockTypeName = options.BlockTypeName;
		if (options.LocationTypeName is not null) _geoIPOptions.LocationTypeName = options.LocationTypeName;
		if (options.IspTypeName is not null) _geoIPOptions.IspTypeName = options.IspTypeName;
		if ((options.UpdateIntervalInHours ?? 0) > 0) _geoIPOptions.UpdateIntervalInHours = options.UpdateIntervalInHours;
		if ((options.RetryIntervalInHours ?? 0) > 0) _geoIPOptions.RetryIntervalInHours = options.RetryIntervalInHours;
		if (options.EnableUpdates is not null) _geoIPOptions.EnableUpdates = options.EnableUpdates;
		if (options.UpdateOnStart is not null) _geoIPOptions.UpdateOnStart = options.UpdateOnStart;
		if (options.LoadOnStart is not null) _geoIPOptions.LoadOnStart = options.LoadOnStart;
		if (options.UseTransientRepositories is not null) _geoIPOptions.UseTransientRepositories = options.UseTransientRepositories;

		_baseTypeNameMappings = new()
		{
			{ _block_base_type_name, _geoIPOptions.BlockTypeName },
			{ _location_base_type_name, _geoIPOptions.LocationTypeName },
			{ _isp_base_type_name, _geoIPOptions.IspTypeName }
		};
	}

	private CsvReposOptions CreateCsvReposOptions(GeoIPOptions options)
	{
		var csvReposOptions = _serializer.DeserializerFromFileOrResource<CsvReposOptions>();

		var files = _maxmindOptions.Files.Where(x => x.Edition == options.MaxmindEdition).ToArray();
		csvReposOptions.Files = files.Select(CreateCsvFileOptions).ToArray();

		Dictionary<string, string> taskProperties = new();
		taskProperties.Add(nameof(GeoIPUpdateTask.OutputDirectory), options.DataDirectory!);
		taskProperties.Add(nameof(GeoIPUpdateTask.MaxmindDownloadUrl), options.MaxmindDownloadUrl!);
		taskProperties.Add(nameof(GeoIPUpdateTask.MaxmindEdition), options.MaxmindEdition!);
		taskProperties.Add(nameof(GeoIPUpdateTask.MaxmindLicenseKey), options.MaxmindLicenseKey!);
		taskProperties.Add(nameof(GeoIPUpdateTask.HashCheckDownload), options.HashCheckDownload!.Value.ToString()!);
		csvReposOptions.UpdateTasks![0].Properties = taskProperties;
		csvReposOptions.UpdateTasks![0].IntervalInMinutes = options.UpdateIntervalInHours!.Value * 60;
		csvReposOptions.UpdateTasks![0].RetryIntervalInMinutes = options.RetryIntervalInHours!.Value * 60;
		csvReposOptions.UpdateTasks![0].AffectedTypeNames = files.Select(x => x.TypeName).ToArray();

		csvReposOptions.OnDataLoaded = OnDataLoaded;

		return csvReposOptions;
	}

	private void OnDataLoaded(DataLoadedEventArgs e)
	{
		if (!e.Type.IsAssignableTo(typeof(ISubnet))) return;
		ISubnet[] data = (ISubnet[])e.Data;
		foreach (var subnet in data)
		{
			var (begin, end) = _networkUtility.GetRangeFromCidr(subnet.Network);
			subnet.BeginAddress = begin;
			subnet.EndAddress = end;
		}
	}

	private CsvFileOptions CreateCsvFileOptions(MaxmindEdititionFileInfo fileInfo)
	{
		string fileName = fileInfo.Name;
		if (fileInfo.BaseTypeName == _location_base_type_name) fileName = fileName.Replace("XX", _geoIPOptions.Language);
		string filePath = _fileSystem.PathCombine(_geoIPOptions.DataDirectory!, fileName);
		_baseTypeNameMappings.TryGetValue(fileInfo.BaseTypeName, out string? itemTypeName);
		itemTypeName ??= fileInfo.TypeName;
		return new CsvFileOptions
		{
			ItemTypeName = itemTypeName,
			FilePath = filePath,
			IsTransientRepository = _geoIPOptions.UseTransientRepositories ?? false
		};
	}
}
