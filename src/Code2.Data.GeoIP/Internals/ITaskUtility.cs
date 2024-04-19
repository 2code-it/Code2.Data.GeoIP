using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal interface ITaskUtility
	{
		Task Delay(TimeSpan timespan, CancellationToken cancellationToken);
	}
}