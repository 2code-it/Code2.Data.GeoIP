using Code2.Data.GeoIP.Models;
using Code2.Tools.Csv.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class GeoIPExtensionsTests
	{
		[TestMethod]
		public void AddGeoIP_When_UsingGenerics_Expect_TypesAsFileInfoOption()
		{
			IServiceCollection services = Substitute.For<IServiceCollection>();
			List<object?> serviceImpl = new List<object?>();

			services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x =>
			{
				serviceImpl.Add(x.Arg<ServiceDescriptor>().ImplementationInstance);
			});
			services.AddGeoIP<EnterpriseBlock, EnterpriseLocation, EnterpriseIsp>();

			CsvReposOptions options = (CsvReposOptions)serviceImpl.Where(x => x is CsvReposOptions).FirstOrDefault()!;
			Assert.IsNotNull(options);
			Assert.IsNotNull(options.Files.FirstOrDefault(x => x.TargetTypeName == nameof(EnterpriseBlock)));
			Assert.IsNotNull(options.Files.FirstOrDefault(x => x.TargetTypeName == nameof(EnterpriseLocation)));
			Assert.IsNotNull(options.Files.FirstOrDefault(x => x.TargetTypeName == nameof(EnterpriseIsp)));
		}

		[TestMethod]
		public void AddGeoIP_When_EnableUpdatesSetToFalse_Expect_NoUpdateTask()
		{
			IServiceCollection services = Substitute.For<IServiceCollection>();
			List<object?> serviceImpl = new List<object?>();

			services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x =>
			{
				serviceImpl.Add(x.Arg<ServiceDescriptor>().ImplementationInstance);
			});
			services.AddGeoIP(x => { x.EnableUpdates = false; });

			CsvReposOptions options = (CsvReposOptions)serviceImpl.Where(x => x is CsvReposOptions).FirstOrDefault()!;
			Assert.IsFalse(options.UpdateTasks.Any());
		}

		[TestMethod]
		public void AddGeoIP_When_EnableUpdatesSetToTrue_Expect_UpdateTaskPropertiesSet()
		{
			IServiceCollection services = Substitute.For<IServiceCollection>();
			List<object?> serviceImpl = new List<object?>();

			services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x =>
			{
				serviceImpl.Add(x.Arg<ServiceDescriptor>().ImplementationInstance);
			});

			string maxmindDownloadUrl = "maxmindDownloadUrl";
			string maxmindLicenseKey = "maxmindLicenseKey";
			string maxmindEdition = "maxmindEdition";
			bool keepDownloadZipFile = true;
			bool hashCheckDownload = true;
			int updateIntervalInhours = 10;

			services.AddGeoIP(x =>
			{
				x.EnableUpdates = true;
				x.MaxmindDownloadUrl = maxmindDownloadUrl;
				x.MaxmindLicenseKey = maxmindLicenseKey;
				x.MaxmindEdition = maxmindEdition;
				x.KeepDownloadedZipFile = keepDownloadZipFile;
				x.HashCheckDownload = hashCheckDownload;
				x.UpdateIntervalInHours = updateIntervalInhours;
			});

			CsvReposOptions options = (CsvReposOptions)serviceImpl.Where(x => x is CsvReposOptions).FirstOrDefault()!;
			Assert.IsTrue(options.UpdateTasks.Any());
			Assert.AreEqual(maxmindDownloadUrl, options.UpdateTasks[0].TaskProperties[nameof(CsvUpdateTaskGeoIP.MaxmindDownloadUrl)]);
			Assert.AreEqual(maxmindLicenseKey, options.UpdateTasks[0].TaskProperties[nameof(CsvUpdateTaskGeoIP.MaxmindLicenseKey)]);
			Assert.AreEqual(maxmindEdition, options.UpdateTasks[0].TaskProperties[nameof(CsvUpdateTaskGeoIP.MaxmindEdition)]);
			Assert.AreEqual(keepDownloadZipFile.ToString(), options.UpdateTasks[0].TaskProperties[nameof(CsvUpdateTaskGeoIP.KeepDownloadedZipFile)]);
			Assert.AreEqual(hashCheckDownload.ToString(), options.UpdateTasks[0].TaskProperties[nameof(CsvUpdateTaskGeoIP.HashCheckDownload)]);
			Assert.AreEqual(updateIntervalInhours, options.UpdateTasks[0].IntervalInHours);
		}
	}
}