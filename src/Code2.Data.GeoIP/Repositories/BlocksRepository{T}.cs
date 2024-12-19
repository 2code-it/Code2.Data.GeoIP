using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP.Repositories
{
	public class BlocksRepository<T> : ICsvRepository<T> where T : ISubnet
	{
		public BlocksRepository(INetworkUtility networkUtility)
		{
			_networkUtility = networkUtility;
		}

		private readonly INetworkUtility _networkUtility;
		private readonly List<IEnumerable<T>> _chunks = new List<IEnumerable<T>>();
		private readonly object _lock = new object();

		public void Add(IEnumerable<T> items)
		{
			lock (_lock)
			{
				foreach (T item in items)
				{
					var range = _networkUtility.GetRangeFromCidr(item.Network);
					item.BeginAddress = range.begin;
					item.EndAddress = range.end;
				}
				_chunks.Add(items);
			}
		}

		public void Clear()
		{
			lock (_lock)
			{
				_chunks.Clear();
			}
		}

		public IEnumerable<T> Get(Func<T, bool>? filter)
		{
			lock (_lock)
			{
				bool filter2(T item) => filter is null || filter(item);
				return _chunks.Where(x => x.Any(filter2)).SelectMany(x => x.Where(filter2));
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
