using Code2.Data.GeoIP.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Code2.Data.GeoIP.Tests
{
	[TestClass]
	public class HttpUtilityTests
	{
		private HttpRequestMessage _requestMessage = default!;
		private HttpRequestMessage _defaultRequestMessage = default!;
		private HttpResponseMessage _responseMessage = default!;
		private IHttpClient _httpClient = default!;
		private HttpRequestHeaders _defaultRequestHeaders = default!;
		private IHttpClientFactory _httpClientFactory = default!;
		private IFileSystem _fileSystem = default!;

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void GetLastModifiedHeaderAsync_When_ResponseNotIsSuccess_Expect_Exception()
		{
			ResetDependencies();
			_responseMessage.StatusCode = System.Net.HttpStatusCode.BadRequest;
			_httpClient.SendAsync(Arg.Any<HttpRequestMessage>()).Returns(_responseMessage);
			_httpClientFactory.Create().Returns(_httpClient);

			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);
			httpUtility.GetLastModifiedHeaderAsync("http://localhost/").Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void GetLastModifiedHeaderAsync_When_LastModifiedHeaderAbsent_Expect_Exception()
		{
			ResetDependencies();
			_httpClient.SendAsync(Arg.Any<HttpRequestMessage>()).Returns(_responseMessage);
			_httpClientFactory.Create().Returns(_httpClient);

			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);
			httpUtility.GetLastModifiedHeaderAsync("http://localhost/").Wait();
		}

		[TestMethod]
		public void GetLastModifiedHeaderAsync_When_HeaderSetCorrect_Expect_CorrectValue()
		{
			ResetDependencies();
			DateTime dateTime = new DateTime(2000, 1, 1, 0, 0, 0);
			_responseMessage.Content.Headers.Add("last-modified", dateTime.ToString("r"));
			_httpClient.SendAsync(Arg.Any<HttpRequestMessage>()).Returns(_responseMessage);
			_httpClientFactory.Create().Returns(_httpClient);

			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);
			DateTime result = httpUtility.GetLastModifiedHeaderAsync("http://localhost/").Result;

			Assert.AreEqual(dateTime.ToLocalTime(), result);
		}

		[TestMethod]
		public void DownloadFileToAsync_When_ResponseStream_Expect_FileStreamWritten()
		{
			ResetDependencies();
			byte[] bufferIn = Encoding.UTF8.GetBytes("0123456789abcdef");
			using MemoryStream msIn = new MemoryStream(bufferIn);
			using MemoryStream msOut = new MemoryStream();
			_httpClient.GetStreamAsync(Arg.Any<string>()).Returns(msIn);
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(msOut);
			_httpClientFactory.Create().Returns(_httpClient);
			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);
			
			httpUtility.DownloadFileToAsync("http://localhost/", "/path/to/file").Wait();

			byte[] bufferOut = msOut.ToArray();
			int compareResult = StructuralComparisons.StructuralComparer.Compare(bufferIn, bufferOut);

			Assert.AreEqual(0, compareResult);
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void DownloadFileToAsync_When_HashMismatch_Expect_Exception()
		{
			ResetDependencies();
			string hash1 = "abcdef";
			string hash2 = "012345";
			using MemoryStream msIn = new MemoryStream();
			using MemoryStream msOut = new MemoryStream();
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(msOut);
			_fileSystem.FileGetSha256Hex(Arg.Any<Stream>()).Returns(hash2);
			_httpClient.GetStreamAsync(Arg.Any<string>()).Returns(msIn);
			_httpClient.GetStringAsync(Arg.Any<string>()).Returns(hash1);
			_httpClientFactory.Create().Returns(_httpClient);
			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);

			httpUtility.DownloadFileToAsync("http://localhost/", "/path/to/file", hash1).Wait();
		}

		[TestMethod]
		public void DownloadFileToAsync_When_HashMatch_Expect_NoProblem()
		{
			ResetDependencies();
			string hash1 = "abcdef";
			using MemoryStream msIn = new MemoryStream();
			using MemoryStream msOut = new MemoryStream();
			_fileSystem.FileCreate(Arg.Any<string>()).Returns(msOut);
			_fileSystem.FileGetSha256Hex(Arg.Any<Stream>()).Returns(hash1);
			_httpClient.GetStreamAsync(Arg.Any<string>()).Returns(msIn);
			_httpClient.GetStringAsync(Arg.Any<string>()).Returns(hash1);
			_httpClientFactory.Create().Returns(_httpClient);
			HttpUtility httpUtility = new HttpUtility(_httpClientFactory, _fileSystem);

			httpUtility.DownloadFileToAsync("http://localhost/", "/path/to/file", hash1).Wait();
		}

		private void ResetDependencies()
		{
			_requestMessage = new HttpRequestMessage();
			_responseMessage = new HttpResponseMessage();
			_defaultRequestMessage = new HttpRequestMessage();
			_httpClient = Substitute.For<IHttpClient>();
			_httpClient.DefaultRequestHeaders.Returns(_defaultRequestMessage.Headers);
			_httpClientFactory = Substitute.For<IHttpClientFactory>();
			_fileSystem = Substitute.For<IFileSystem>();
			
		}
	}
}
