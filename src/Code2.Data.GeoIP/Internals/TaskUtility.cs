using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal class TaskUtility : ITaskUtility
	{
		public Task Delay(TimeSpan timespan, CancellationToken cancellationToken)
			=> Task.Delay(timespan, cancellationToken).ContinueWith(x=>Task.CompletedTask);
	}
}
