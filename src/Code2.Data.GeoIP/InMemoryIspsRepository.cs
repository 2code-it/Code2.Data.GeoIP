using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP
{
	public class InMemoryIspsRepository<T> : IRepository<T, int>
		where T : IspBase
	{
		private readonly List<T> _items = new List<T>();

		public bool HasData => _items.Count > 0;

		public IEnumerable<T> Get(Func<T, bool>? filter = null)
			=> filter is null ? _items : _items.Where(filter);

		public T? GetSingle(int id)
			=> _items.FirstOrDefault(x => x.IspId == id);

		public void Add(IEnumerable<T> items)
			=> _items.AddRange(items);

		public void RemoveAll()
		{
			_items.Clear();
		}
	}
}
