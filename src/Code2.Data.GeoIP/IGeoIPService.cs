﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public interface IGeoIPService<Tblock, Tlocation>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		bool HasData { get; }
		bool IsUpdating { get; }
		GeoIPServiceOptions Options { get; }

		event EventHandler<UnhandledExceptionEventArgs>? Error;
		event EventHandler? Update;

		void Configure(Action<GeoIPServiceOptions> configure);
		void Configure(GeoIPServiceOptions options);
		Tblock? GetBlock(string ipAddress);
		Tblock? GetBlock(UInt128 ipNumber);
		IEnumerable<Tblock> GetBlocks(Func<Tblock, bool> filter);
		Tlocation? GetLocation(int geoNameId);
		IEnumerable<Tlocation> GetLocations(Func<Tlocation, bool> filter);
		Task LoadAsync();
		Task UpdateFilesAsync();
	}
}