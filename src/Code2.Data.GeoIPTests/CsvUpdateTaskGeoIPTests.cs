using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class CsvUpdateTaskGeoIPTests
	{
		[TestMethod]
		[DataRow(-1, 0, true)]
		[DataRow(1, 0, false)]
		public async Task CanRunAsync_When_LocalFileOutdated_Expect_TrueElseFalse(int localFileDateDays, int remoteDateDays, bool expectedResult)
		{
			DateTime date = new DateTime(2001, 1, 1);
			IFileSystem fileSystem = Substitute.For<IFileSystem>();
			IHttpUtility httpUtility = Substitute.For<IHttpUtility>();
			CsvReposOptions csvReposOptions = new();
			fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
			fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "file1" });
			fileSystem.FileGetLastWriteTime(Arg.Any<string>()).Returns(date.AddDays(localFileDateDays));
			httpUtility.GetLastModifiedHeaderAsync(Arg.Any<string>()).Returns(Task.FromResult(date.AddDays(remoteDateDays)));

			CsvUpdateTaskGeoIP updateTask = new CsvUpdateTaskGeoIP(csvReposOptions, fileSystem, httpUtility);

			bool result = await updateTask.CanRunAsync();

			Assert.AreEqual(expectedResult, result);
		}

		[TestMethod]
		public async Task RunAsync_When_Invoked_Expect_TrueElseFalse()
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();
			IHttpUtility httpUtility = Substitute.For<IHttpUtility>();
			CsvReposOptions csvReposOptions = new();

			fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
			fileSystem.DirectoryGetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new[] { "file1" });
			fileSystem.PathCombine(Arg.Any<string[]>()).Returns("zipfile");
			fileSystem.FileExists(Arg.Any<string>()).Returns(false);
			csvReposOptions.Files.Add(new CsvFileInfo { NameFilter = "location.csv" });

			CsvUpdateTaskGeoIP updateTask = new CsvUpdateTaskGeoIP(csvReposOptions, fileSystem, httpUtility);
			updateTask.HashCheckDownload = false;

			await updateTask.RunAsync();

			fileSystem.Received(1).ZipArchiveExtractEntryTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
		}
	}
}