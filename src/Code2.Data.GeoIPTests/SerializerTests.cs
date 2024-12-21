using Code2.Data.GeoIP.Internals;
using Code2.Data.GeoIPTests.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Code2.Data.GeoIPTests;

[TestClass]
public class SerializerTests
{

	[TestMethod]
	public void DeserializerFromFileOrResource_When_TypeFileExists_Expect_FileOpenRead()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		Serializer serializer = new Serializer(fileSystem);
		using Stream stream = GetStreamFromText("{}");
		fileSystem.FileExists(Arg.Any<string>()).Returns(true);
		fileSystem.FileOpenRead(Arg.Any<string>()).Returns(stream);

		var item = serializer.DeserializerFromFileOrResource<TestItem>();

		fileSystem.Received(1).FileOpenRead(Arg.Any<string>());
	}

	[TestMethod]
	public void DeserializerFromFileOrResource_When_TypeFileNotExists_Expect_ResourceOpenRead()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		Serializer serializer = new Serializer(fileSystem);
		using Stream stream = GetStreamFromText("{}");
		fileSystem.FileExists(Arg.Any<string>()).Returns(false);
		fileSystem.GetManifestResourceStream(Arg.Any<string>()).Returns(stream);

		var item = serializer.DeserializerFromFileOrResource<TestItem>();

		fileSystem.Received(1).GetManifestResourceStream(Arg.Any<string>());
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void DeserializerFromFileOrResource_When_FileOrResourceContainsInvalidJson_Expect_Exception()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		Serializer serializer = new Serializer(fileSystem);
		using Stream stream = GetStreamFromText("{\"name\"=1}");
		fileSystem.FileExists(Arg.Any<string>()).Returns(true);
		fileSystem.FileOpenRead(Arg.Any<string>()).Returns(stream);

		var item = serializer.DeserializerFromFileOrResource<TestItem>();
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DeserializerFromFileOrResource_When_TypeNotConfigured_Expect_Exception()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();
		Serializer serializer = new Serializer(fileSystem);
		fileSystem.FileExists(Arg.Any<string>()).Returns(false);
		fileSystem.GetManifestResourceStream(Arg.Any<string>()).Returns((Stream?)null);

		var item = serializer.DeserializerFromFileOrResource<TestItem>();
	}


	private Stream GetStreamFromText(string text)
	{
		MemoryStream ms = new MemoryStream();
		ms.Write(Encoding.UTF8.GetBytes(text));
		ms.Position = 0;
		return ms;
	}
}
