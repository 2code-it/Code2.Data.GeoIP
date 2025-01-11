using Code2.Data.GeoIP.Repositories;
using Code2.Tools.Csv.Repos;
using System;

namespace Code2.Data.GeoIP;
public static class RepositoryExtensions
{
	private static readonly NetworkUtility _networkUtility = new();

	public static Tblock? GetBlock<Tblock>(this ICsvRepository<Tblock> repository, string ipAddress)
			where Tblock : ISubnet
	{
		UInt128 ipNumber = _networkUtility.GetIpNumberFromAddress(ipAddress);
		BlocksRepository<Tblock> blocksRepository = (BlocksRepository<Tblock>)repository;
		return blocksRepository.GetBlock(ipNumber);
	}
}
