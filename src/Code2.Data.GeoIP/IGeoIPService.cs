using System;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public interface IGeoIPService<Tblock, Tlocation>
		where Tblock : BlockBase, new()
		where Tlocation : LocationBase, new()
	{
		GeoIPServiceOptions Options { get; }

		Tblock? GetBlock(UInt128 ipNumber);
		Tblock? GetBlock(string ipAddress);
		Tlocation? GetLocation(int geoNameId);
		void Load();
		void UpdateFiles();
		void UpdateFiles(string zipFilePath);
		Task LoadAsync();
		Task UpdateFilesAsync();
		Task UpdateFilesAsync(string zipFilePath);
	}
}