using Code2.Data.GeoIP.Models;

namespace Code2.Data.GeoIP.Repositories;
public class EnterpriseBlocksRepository : BlocksRepository<EnterpriseBlock>
{
	public EnterpriseBlocksRepository(INetworkUtility networkUtility) : base(networkUtility) { }
}
