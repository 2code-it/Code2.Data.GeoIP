using Code2.Data.GeoIP;
using Code2.Data.GeoIP.Internals;
using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Code2.Data.GeoIPTests.Assets;
public class TestGeoIPUpdateTask : GeoIPUpdateTask
{
	internal TestGeoIPUpdateTask(IFileSystem fileSystem) : base(fileSystem)
	{
	}

	public Func<string, bool> MockFileExists { get; set; } = (p) => false;
	public Func<string, string> MockPathGetFullPath { get; set; } = (p) => string.Empty;
	public Func<string[], string> MockPathCombine { get; set; } = (p) => string.Empty;
	public Func<string, DateTime> MockFileLastWriteTime { get; set; } = (p) => DateTime.MinValue;
	public Action<string> MockFileDelete { get; set; } = (p) => { };
	public Func<string, bool> MockDirectoryExists { get; set; } = (p) => true;
	public Action<string> MockDirectoryCreate { get; set; } = (p) => { };
	public Func<string, Dictionary<string, string>?, Task<Dictionary<string, string>>> MockGetHeadersOnlyAsync { get; set; } = (u, h) => Task.FromResult(new Dictionary<string, string>());
	public Func<string, Dictionary<string, string>?, Task<byte[]>> MockGetByteArrayAsync { get; set; } = (u, h) => Task.FromResult(Array.Empty<byte>());

	public IResult? MockBeforeRun()
		=> OnBeforeRun();

	public IResult? MockAfterRun()
		=> OnAfterRun();

	protected override bool FileExists(string filePath)
		=> MockFileExists(filePath);

	protected override void FileDelete(string filePath)
		=> MockFileDelete(filePath);

	protected override string PathCombine(params string[] paths)
		=> MockPathCombine(paths);

	protected override string PathGetFullPath(string path)
		=> MockPathGetFullPath(path);

	protected override DateTime FileLastWriteTime(string filePath)
		=> MockFileLastWriteTime(filePath);

	protected override bool DirectoryExists(string path)
		=> MockDirectoryExists(path);

	protected override void DirectoryCreate(string path)
		=> MockDirectoryCreate(path);

	protected override async Task<Dictionary<string, string>> HttpGetHeadersOnlyAsync(string url, Dictionary<string, string>? requestHeaders = null)
		=> await MockGetHeadersOnlyAsync(url, requestHeaders);

	protected override async Task<byte[]> HttpGetByteArrayAsync(string url, Dictionary<string, string>? requestHeaders = null)
		=> await MockGetByteArrayAsync(url, requestHeaders);
}
