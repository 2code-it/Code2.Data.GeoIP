using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP.Internals
{
	internal class TaskUtility : ITaskUtility
	{
		public Task Delay(int milliseconds, CancellationToken cancellationToken)
			=> Task.Delay(milliseconds, cancellationToken);
	}
}
