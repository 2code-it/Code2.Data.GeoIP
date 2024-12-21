using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Internals;
using Code2.Data.GeoIPTests.Assets;
using Code2.Tools.Csv.Repos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIPTests;

[TestClass]
public class GeoIPUpdateTaskTests
{
	[TestMethod]
	public void OnBeforeRun_When_MaxmindPropertiesSet_Expect_UrlParameterReplacement()
	{
		string licenseKey = "key123";
		string edition = "edition1";
		string urlTemplate = $"http://www/?edition=$({nameof(GeoIPUpdateTask.MaxmindEdition)})&license=$({nameof(GeoIPUpdateTask.MaxmindLicenseKey)})";
		string urlExpected = $"http://www/?edition={edition}&license={licenseKey}";
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		updateTask.MaxmindDownloadUrl = urlTemplate;
		updateTask.MaxmindEdition = edition;
		updateTask.MaxmindLicenseKey = licenseKey;

		updateTask.MockBeforeRun();

		Assert.AreEqual(urlExpected, updateTask.Url);
	}

	[TestMethod]
	public void OnBeforeRun_When_OutputDirectoryDoesNotExist_Expect_OutputDirectoryCreated()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		bool createReceived = false;
		updateTask.OutputDirectory = "./data";
		updateTask.MockDirectoryExists = (p) => false;
		updateTask.MockDirectoryCreate = (p) => createReceived = true;

		updateTask.MockBeforeRun();

		Assert.IsTrue(createReceived);
	}

	[TestMethod]
	public void OnBeforeRun_When_FileExists_Expect_FileLastWriteTimeCall()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		bool lastWriteTimeReceived = false;
		updateTask.MockFileExists = (p) => true;
		updateTask.MockFileLastWriteTime = (p) => { lastWriteTimeReceived = true; return DateTime.Now; };

		updateTask.MockBeforeRun();

		Assert.IsTrue(lastWriteTimeReceived);
	}

	[TestMethod]
	public void OnBeforeRun_When_LastmodifiedHeaderMissing_Expect_ResultError()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);

		var result = updateTask.MockBeforeRun();

		Console.WriteLine("Result.Message: {0}", result?.Message);
		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Error, result.State);
	}

	[TestMethod]
	public void OnBeforeRun_When_LastmodifiedHeaderValueInvalid_Expect_ResultError()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		string lastModified = new DateTime(2001, 1, 1, 12, 0, 0).ToString("s");
		updateTask.MockGetHeadersOnlyAsync = (u, h) => Task.FromResult(new Dictionary<string, string>() { { "last-modified", lastModified } });

		var result = updateTask.MockBeforeRun();

		Console.WriteLine("Result.Message: {0}", result?.Message);
		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Error, result.State);
	}

	[TestMethod]
	public void OnBeforeRun_When_RemoteLastmodifiedIsGreaterThanLocal_Expect_ResultSuccess()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		DateTime local = new DateTime(2001, 1, 1, 12, 0, 0);
		string remoteString = local.ToUniversalTime().AddHours(1).ToString("r");
		updateTask.MockFileExists = (p) => true;
		updateTask.MockFileLastWriteTime = (p) => local;
		updateTask.MockGetHeadersOnlyAsync = (u, h) => Task.FromResult(new Dictionary<string, string>() { { "last-modified", remoteString } });

		var result = updateTask.MockBeforeRun();

		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Success, result.State);
	}

	[TestMethod]
	public void OnBeforeRun_When_RemoteLastmodifiedEqualsLocal_Expect_ResultCancelled()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		DateTime localDateTime = new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Local);
		DateTime remoteDateTime = localDateTime.ToUniversalTime();
		string lastModifiedString = remoteDateTime.ToString("r");
		updateTask.MockFileExists = (p) => true;
		updateTask.MockFileLastWriteTime = (p) => localDateTime;
		updateTask.MockGetHeadersOnlyAsync = (u, h) => Task.FromResult(new Dictionary<string, string>() { { "last-modified", lastModifiedString } });

		var result = updateTask.MockBeforeRun();

		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Cancelled, result.State);
	}

	[TestMethod]
	public void OnBeforeRun_When_ErrorOccurs_Expect_ResultError()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		updateTask.MockGetHeadersOnlyAsync = (u, h) => throw new InvalidOperationException();

		var result = updateTask.MockBeforeRun();

		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Error, result.State);
	}

	[TestMethod]
	public void OnAfterRun_When_HashCheckDownloadAndHashMismatch_Expect_FileDeleteAndResultCancel()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		string hashRemote = "A0B1C3";
		string hashLocal = "A0B1C2";
		bool fileDeleteReceived = false;
		fileSystem.FileGetSha256Hex(Arg.Any<string>()).Returns(hashLocal);
		updateTask.MockGetByteArrayAsync = (u, h) => Task.FromResult(Encoding.UTF8.GetBytes(hashRemote));
		updateTask.MockFileDelete = (p) => fileDeleteReceived = true;
		updateTask.HashCheckDownload = true;

		var result = updateTask.MockAfterRun();

		Assert.IsTrue(fileDeleteReceived);
		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Cancelled, result.State);
	}

	[TestMethod]
	public void OnAfterRun_When_HashCheckDownloadAndHashMatch_Expect_ResultSuccess()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		string hashRemote = "A0B1C2";
		string hashLocal = "A0B1C2";
		fileSystem.FileGetSha256Hex(Arg.Any<string>()).Returns(hashLocal);
		updateTask.MockGetByteArrayAsync = (u, h) => Task.FromResult(Encoding.UTF8.GetBytes(hashRemote));
		updateTask.HashCheckDownload = true;

		var result = updateTask.MockAfterRun();

		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Success, result.State);
	}

	[TestMethod]
	public void OnAfterRun_When_ErrorOccurs_Expect_ResultError()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		TestGeoIPUpdateTask updateTask = new TestGeoIPUpdateTask(fileSystem);
		updateTask.MockPathGetFullPath = (p) => throw new InvalidOperationException();


		var result = updateTask.MockAfterRun();

		Assert.IsNotNull(result);
		Assert.AreEqual(ResultState.Error, result.State);
	}
}
