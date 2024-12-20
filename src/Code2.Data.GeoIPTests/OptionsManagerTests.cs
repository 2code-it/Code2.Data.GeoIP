using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq;

namespace Code2.Data.GeoIPTests;

[TestClass]
public class OptionsManagerTests
{
	[TestMethod]
	public void Configure_When_GeoIPPropertiesSet_Expect_CsvReposPropertiesSet()
	{
		ISerializer serializer = GetSerializerSubstituteWithDefaultOptions();
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		OptionsManager optionsManager = new OptionsManager(serializer, fileSystem);
		GeoIPOptions geoIPOptions = GetDefaultGeoIPOptions();
		MaxmindMetaOptions maxmindOptions = GetDefaultMaxmindMetaOptions();
		geoIPOptions.Language = "de";
		geoIPOptions.MaxmindEdition = "edition1";

		optionsManager.Configure(geoIPOptions);
		var csvReposOptions = optionsManager.GetCsvReposOptions();


		Assert.AreEqual(geoIPOptions.UpdateIntervalInHours * 60, csvReposOptions.UpdateTasks![0].IntervalInMinutes);
		Assert.AreEqual(geoIPOptions.RetryIntervalInHours * 60, csvReposOptions.UpdateTasks![0].RetryIntervalInMinutes);
		Assert.AreEqual(maxmindOptions.Files.Count(x => x.Edition == geoIPOptions.MaxmindEdition), csvReposOptions.Files!.Length);
	}

	[TestMethod]
	public void Configure_When_LanguageIsSet_Expect_LanguageSpecificFileRename()
	{
		ISerializer serializer = GetSerializerSubstituteWithDefaultOptions();
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		OptionsManager optionsManager = new OptionsManager(serializer, fileSystem);
		GeoIPOptions geoIPOptions = GetDefaultGeoIPOptions();
		MaxmindMetaOptions maxmindOptions = GetDefaultMaxmindMetaOptions();
		geoIPOptions.Language = "de";
		geoIPOptions.MaxmindEdition = "edition1";
		fileSystem.PathCombine(Arg.Any<string[]>()).Returns(x => string.Join("", x.Arg<string[]>()));

		optionsManager.Configure(geoIPOptions);
		var csvReposOptions = optionsManager.GetCsvReposOptions();

		var languageFileMaxmind = maxmindOptions.Files.FirstOrDefault(x => x.Edition == geoIPOptions.MaxmindEdition && x.Name.Contains("XX"));
		Assert.IsNotNull(languageFileMaxmind);
		var languageFileCsvRepos = csvReposOptions.Files!.FirstOrDefault(x => x.ItemTypeName == languageFileMaxmind.TypeName);
		Assert.IsNotNull(languageFileCsvRepos);
		Assert.IsTrue(languageFileCsvRepos.FilePath.Contains(geoIPOptions.Language));
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Configure_When_MaxmindEditionNotAvailable_Expect_Exception()
	{
		ISerializer serializer = GetSerializerSubstituteWithDefaultOptions();
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		OptionsManager optionsManager = new OptionsManager(serializer, fileSystem);
		GeoIPOptions geoIPOptions = GetDefaultGeoIPOptions();
		geoIPOptions.MaxmindEdition = "edition3";

		optionsManager.Configure(geoIPOptions);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Configure_When_LanguageNotAvailable_Expect_Exception()
	{
		ISerializer serializer = GetSerializerSubstituteWithDefaultOptions();
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		OptionsManager optionsManager = new OptionsManager(serializer, fileSystem);
		GeoIPOptions geoIPOptions = GetDefaultGeoIPOptions();
		geoIPOptions.MaxmindEdition = "xx";

		optionsManager.Configure(geoIPOptions);
	}

	[TestMethod]
	public void Configure_When_TypeNameOptionsAreSet_Expect_AlternateFileType()
	{
		ISerializer serializer = GetSerializerSubstituteWithDefaultOptions();
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		OptionsManager optionsManager = new OptionsManager(serializer, fileSystem);
		GeoIPOptions geoIPOptions = GetDefaultGeoIPOptions();
		geoIPOptions.BlockTypeName = "TestBlock";
		geoIPOptions.LocationTypeName = "TestLocation";
		geoIPOptions.MaxmindEdition = "edition1";

		optionsManager.Configure(geoIPOptions);
		var csvReposOptions = optionsManager.GetCsvReposOptions();

		Assert.AreEqual(geoIPOptions.BlockTypeName, csvReposOptions.Files![0].ItemTypeName);
		Assert.AreEqual(geoIPOptions.LocationTypeName, csvReposOptions.Files![1].ItemTypeName);
	}

	private static ISerializer GetSerializerSubstituteWithDefaultOptions()
	{
		var serializer = Substitute.For<ISerializer>();
		serializer.DeserializerFromFileOrResource<GeoIPOptions>().Returns(GetDefaultGeoIPOptions());
		serializer.DeserializerFromFileOrResource<CsvReposOptions>().Returns(GetDefaultCsvReposOptions());
		serializer.DeserializerFromFileOrResource<MaxmindMetaOptions>().Returns(GetDefaultMaxmindMetaOptions());
		return serializer;
	}

	private static GeoIPOptions GetDefaultGeoIPOptions() => new()
	{
		DataDirectory = "./data",
		MaxmindLicenseKey = "license1",
		MaxmindEdition = "edition1",
		MaxmindDownloadUrl = "http://download/",
		HashCheckDownload = false,
		Language = "es",
		UpdateIntervalInHours = 12,
		RetryIntervalInHours = 4,
		EnableUpdates = true,
		UpdateOnStart = true,
		LoadOnStart = true
	};

	private static CsvReposOptions GetDefaultCsvReposOptions() => new()
	{
		DefaultReaderOptions = new()
		{
			HasHeaderRow = true,
			IgnoreEmptyWhenDeserializing = true
		},
		ReaderReadSize = 3000,
		UpdateIntervalInMinutes = 5,
		RetryIntervalInMinutes = 85,
		UpdateTasks = new[] { new CsvUpdateTaskOptions
			{
				TaskTypeName = "TestUpdateTask",
				IntervalInMinutes =  600,
				RetryIntervalInMinutes = 72
			}
		}
	};

	private static MaxmindMetaOptions GetDefaultMaxmindMetaOptions() => new()
	{
		UpdateIntervalInHours = 12,
		DownloadUrl = "http://download.it/",
		DefaultLanguage = "es",
		SupportedLanguages = new string[] { "en", "de", "es" },
		Files = new[]
		{
			new MaxmindEdititionFileInfo
			{
				Edition = "edition1",
				TypeName = "Edition1Part1Type",
				BaseTypeName = "BlockBase",
				Name = "part1.csv",
			},
			new MaxmindEdititionFileInfo
			{
				Edition = "edition1",
				TypeName = "Edition1Part2Type",
				BaseTypeName = "LocationBase",
				Name = "part2-XX.csv",
			},
			new MaxmindEdititionFileInfo
			{
				Edition = "edition2",
				TypeName = "Edition2Part1Type",
				BaseTypeName = "BlockBase",
				Name = "part1.csv",
			},
			new MaxmindEdititionFileInfo
			{
				Edition = "edition2",
				TypeName = "Edition2Part2Type",
				BaseTypeName = "LocationBase",
				Name = "part2-XX.csv",
			}
		}
	};
}
