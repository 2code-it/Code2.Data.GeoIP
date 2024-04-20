using System;
using System.Collections.Generic;

namespace Code2.Data.GeoIP
{
	public interface IRepository<T, Tid>
	{
		bool HasData { get; }
		void Add(IEnumerable<T> items);
		//void AddSingle(T item);
		IEnumerable<T> Get(Func<T, bool>? filter = null);
		T? GetSingle(Tid id);
		//void Remove(IEnumerable<Tid> ids);
		//void RemoveSingle(Tid id);
		void RemoveAll();
	}
}
