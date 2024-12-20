using System;
using System.IO;
using System.Text.Json;

namespace Code2.Data.GeoIP.Internals;
internal class Serializer : ISerializer
{
	public Serializer() : this(new FileSystem())
	{ }
	internal Serializer(IFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}

	private readonly IFileSystem _fileSystem;

	public T DeserializerFromFileOrResource<T>()
	{
		Type type = typeof(T);
		string fileName = $"{type.Name}.json";
		string filePath = _fileSystem.PathGetFullPath($"./{fileName}");
		using Stream? stream = _fileSystem.FileExists(filePath) ? _fileSystem.FileOpenRead(filePath) : _fileSystem.GetManifestResourceStream(fileName);
		if (stream is null) throw new InvalidOperationException($"{type.Name} not configured as resource or file");
		return JsonSerializer.Deserialize<T>(stream) ?? throw new InvalidOperationException($"{type.Name} is null");
	}
}
