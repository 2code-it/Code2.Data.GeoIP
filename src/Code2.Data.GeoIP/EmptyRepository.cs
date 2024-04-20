using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2.Data.GeoIP
{
	public class EmptyRepository<T, Tid> : IRepository<T, Tid>
	{
		public bool HasData => false;
		public void Add(IEnumerable<T> items){}
		public IEnumerable<T> Get(Func<T, bool>? filter = null) => Array.Empty<T>();
		public T? GetSingle(Tid id) => default;
		public void RemoveAll(){}
	}
}
