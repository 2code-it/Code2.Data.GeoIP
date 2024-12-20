using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Data.GeoIP.Repositories;

public class Repository<T> : ICsvRepository<T>
{
	private readonly List<T> _items = new List<T>();
	private readonly object _lock = new object();

	public void Add(IEnumerable<T> items)
	{
		lock (_lock)
		{
			_items.AddRange(items);
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			_items.Clear();
		}
	}

	public IEnumerable<T> Get(Func<T, bool>? filter = null)
	{
		lock (_lock)
		{
			return _items.Where(x => filter is null || filter(x)).ToArray();
		}
	}
}
