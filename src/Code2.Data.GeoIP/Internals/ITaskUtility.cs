using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal interface ITaskUtility
	{
		Task Delay(int milliseconds, CancellationToken cancellationToken);
	}
}