using System.Collections.Generic;
using System.Linq.Expressions;

namespace Code2.Data.GeoIP
{
	public class InMemoryBlocksRepository<T> : IRepository<T, UInt128>
		where T : BlockBase
	{
		private readonly List<T[]> _items = new List<T[]>();
		
		public IEnumerable<T> Get(Func<T, bool>? filter = null)
			=> filter is null?_items.SelectMany(x=>x): _items.SelectMany(x => x).Where(filter);

		public T? GetSingle(UInt128 id)
		{
			return _items.FirstOrDefault(x => x.First().BeginAddress <= id && x.Last().EndAddress >= id)
				?.FirstOrDefault(x => x.BeginAddress <= id && x.EndAddress >= id);
		}

		public void Add(IEnumerable<T> items)
		{
			_items.Add(items.ToArray());
		}

		public void AddSingle(T item)
		{
			throw new NotImplementedException();
		}

		public void Remove(IEnumerable<UInt128> ids)
		{
			throw new NotImplementedException();
		}

		public void RemoveSingle(UInt128 id)
		{
			throw new NotImplementedException();
		}
	}
}
