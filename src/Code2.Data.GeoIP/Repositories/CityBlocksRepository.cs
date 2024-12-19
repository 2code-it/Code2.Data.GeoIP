using Code2.Data.GeoIP.Models;

namespace Code2.Data.GeoIP.Repositories;

public class CityBlocksRepository : BlocksRepository<CityBlock>
{
	public CityBlocksRepository(INetworkUtility networkUtility) : base(networkUtility) { }
}
