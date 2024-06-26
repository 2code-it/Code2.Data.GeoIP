﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Code2.Data.GeoIP.Internals
{
	internal interface IFileSystem
	{
		StreamReader FileOpenText(string path);
		Stream FileOpenRead(string path);
		Stream FileOpen(string path);
		Stream FileCreate(string path);
		void FileDelete(string path);
		bool FileExists(string path);
		string FileReadAllText(string path);
		DateTime FileGetLastWriteTime(string path);
		void FileAppendAllLines(string path, IEnumerable<string> contents);
		string PathGetFullPath(string path);
		string PathCombine(params string[] paths);
		string[] DirectoryGetFiles(string path, string search);
		void DirectoryCreate(string path);
		bool DirectoryExists(string path);
		string FileGetSha256Hex(Stream stream);
		void ZipArchiveExtractEntryTo(string zipFilePath, string fileFilter, string outputDirectory);
	}
}