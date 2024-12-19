using Code2.Data.GeoIP.Models;

namespace Code2.Data.GeoIP.Repositories;
public class CountryBlocksRepository : BlocksRepository<CountryBlock>
{
	public CountryBlocksRepository(INetworkUtility networkUtility) : base(networkUtility) { }
}
