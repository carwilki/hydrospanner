namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public class DuplicateStore
	{
		public bool Contains(Guid key)
		{
			if (key == Guid.Empty)
				return false;

			if (this.entries.Contains(key))
				return true;

			while (this.cache.Count >= this.capacity)
				this.entries.Remove(this.cache.Dequeue());

			this.entries.Add(key);
			this.cache.Enqueue(key);

			return false;
		}

		public DuplicateStore(int capacity)
		{
			this.capacity = capacity;
			this.entries = new HashSet<Guid>();
		}

		private readonly Queue<Guid> cache = new Queue<Guid>();
		private readonly HashSet<Guid> entries; 
		private readonly long capacity;
	}
}