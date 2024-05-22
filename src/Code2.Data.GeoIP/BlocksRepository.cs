using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP
{
	public class BlocksRepository<T> : IRepository<T> where T : ISubnet
	{
		private readonly List<IEnumerable<T>> _chunks = new List<IEnumerable<T>>();
		private readonly object _lock = new object();

		public virtual void Add(IEnumerable<T> items)
		{
			lock (_lock)
			{
				_chunks.Add(items);
			}
		}

		public virtual void Clear()
		{
			lock (_lock)
			{
				_chunks.Clear();
			}
		}

		public virtual IEnumerable<T> Get(Func<T, bool> filter)
		{
			lock (_lock)
			{
				return _chunks.Where(x => x.Any(filter)).SelectMany(x => x.Where(filter));
			}
		}

		public T? GetBlock(UInt128 ipNumber)
		{
			lock (_lock)
			{
				return _chunks.Where(x => x.First().BeginAddress <= ipNumber && x.Last().EndAddress >= ipNumber).SelectMany(x => x)
					.FirstOrDefault(x => x.BeginAddress <= ipNumber && x.EndAddress >= ipNumber);
			}
		}
	}
}
