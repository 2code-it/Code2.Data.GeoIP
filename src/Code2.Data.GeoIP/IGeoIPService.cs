﻿using System;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public interface IGeoIPService<Tblock, Tlocation>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		GeoIPServiceOptions Options { get; }
		bool HasData { get; }

		Tblock? GetBlock(UInt128 ipNumber);
		Tblock? GetBlock(string ipAddress);
		Tlocation? GetLocation(int geoNameId);
		void Load();
		void UpdateFiles();
		void UpdateFiles(string zipFilePath);
		DateTime GetLastFileWriteTime();
		Task LoadAsync();
		Task UpdateFilesAsync();
		Task UpdateFilesAsync(string zipFilePath);

		void Configure(Action<GeoIPServiceOptions> configure);
		void Configure(GeoIPServiceOptions options);
	}
}