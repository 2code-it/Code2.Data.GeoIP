using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class CsvUpdateServiceTests
	{
		private IFileSystem _fileSystem = default!;
		private IHttpUtility _httpUtility = default!;
		private ITaskUtility _taskUtility = default!;
		private const int _msPerHour = 3600000;

		[TestMethod]
		public void UpdateFilesAsync_When_FileUpdate_Expect_CorrectIsUpdatingValue()
		{
			ResetDependencies();
			_fileSystem.FileExists(Arg.Any<string>()).Returns(false);
			_httpUtility.DownloadFileToAsync(Arg.Any<string>(), Arg.Any<string>(), default).ReturnsForAnyArgs(x => Task.Delay(10));
			CsvUpdateService csvUpdateService = new CsvUpdateService(_fileSystem, _httpUtility, _taskUtility);

			csvUpdateService.UpdateFilesAsync().Wait(0);
			bool shouldBeTrue = csvUpdateService.IsUpdating;
			Thread.Sleep(50);
			bool shouldBeFalse = csvUpdateService.IsUpdating;

			Assert.IsTrue(shouldBeTrue);
			Assert.IsFalse(shouldBeFalse);
		}

		[TestMethod()]
		[DataRow(-75, -76, 6)]	//local file older than 3 days but no newer remote file, expect 6 hour delay
		[DataRow(-24, -76, 24)]	//day old local file but no newer remote file, expect 24 hour delay
		[DataRow(-26, -2, 72)]	//newer remote file, runs update and delays 72 hours
		public void StartAutomaticUpdating_When_FileLastModifiedComparedToRemote_Expect_Delay(int addHoursFile, int addHoursRemote, int expectedHoursDelay)
		{
			Console.WriteLine("addHoursFile: {0}, addHoursRemote: {1}, expectedHoursDelay: {2}", addHoursFile, addHoursRemote, expectedHoursDelay);
			ResetDependencies();
			DateTime dateSource = new DateTime(2000, 10, 10, 11,0,0);
			DateTime lastModifiedLocal = dateSource.AddHours(addHoursFile);
			DateTime lastModifiedRemote = dateSource.AddHours(addHoursRemote);
			_fileSystem.FileExists(Arg.Any<string>()).Returns(false);
			_fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "blockipv4.csv" });
			_fileSystem.FileGetLastWriteTime(Arg.Any<string>()).Returns(lastModifiedLocal);
			_httpUtility.GetLastModifiedHeaderAsync(Arg.Any<string>()).Returns(lastModifiedRemote);
			_taskUtility.Delay(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.Delay(10, (CancellationToken)x[1]));
			CsvUpdateService csvUpdateService = new CsvUpdateService(_fileSystem, _httpUtility, _taskUtility);
			csvUpdateService.CsvFileFilters = new[] { "blockipv4" };
			
			csvUpdateService.StartAutomaticUpdating();
			Thread.Sleep(5);
			csvUpdateService.StopAutomaticUpdating();

			_taskUtility.Received(1).Delay(_msPerHour * expectedHoursDelay, Arg.Any<CancellationToken>());
		}

		[TestMethod()]
		public void StartAutomaticUpdating_When_ExceptionOccurs_Expect_Delay1Hour()
		{
			ResetDependencies();
			_httpUtility.GetLastModifiedHeaderAsync(Arg.Any<string>()).Throws(new InvalidOperationException());
			_taskUtility.Delay(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.Delay(10, (CancellationToken)x[1]));
			CsvUpdateService csvUpdateService = new CsvUpdateService(_fileSystem, _httpUtility, _taskUtility);
			csvUpdateService.Error += (_, _) => { };

			csvUpdateService.StartAutomaticUpdating();
			Thread.Sleep(5);
			csvUpdateService.StopAutomaticUpdating();

			_taskUtility.Received(1).Delay(_msPerHour, Arg.Any<CancellationToken>());
		}

		private void ResetDependencies()
		{
			_fileSystem = Substitute.For<IFileSystem>();
			_httpUtility = Substitute.For<IHttpUtility>();
			_taskUtility = Substitute.For<ITaskUtility>();
		}
	}
}
