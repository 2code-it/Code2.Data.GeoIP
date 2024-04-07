using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP
{
	public class InMemoryLocationsRepository<T> : IRepository<T, int>
		where T : LocationBase
	{
		private readonly List<T> _items = new List<T>();

		public bool HasData => _items.Count > 0;

		public IEnumerable<T> Get(Func<T, bool>? filter = null)
			=> filter is null ? _items : _items.Where(filter);

		public T? GetSingle(int id)
			=> _items.FirstOrDefault(x => x.GeoNameId == id);

		public void Add(IEnumerable<T> items)
			=> _items.AddRange(items);

		public void AddSingle(T item)
		{
			throw new NotImplementedException();
		}

		public void Remove(IEnumerable<int> ids)
		{
			throw new NotImplementedException();
		}

		public void RemoveSingle(int id)
		{
			throw new NotImplementedException();
		}

		public void RemoveAll()
		{
			_items.Clear();
		}
	}
}
