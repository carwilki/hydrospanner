namespace Hydrospanner.Phases.Transformation
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

			this.Add(key);
			return false;
		}
		private void Add(Guid key)
		{
			while (this.cache.Count >= this.capacity)
				this.entries.Remove(this.cache.Dequeue());

			this.entries.Add(key);
			this.cache.Enqueue(key);
		}

		public DuplicateStore(int capacity) : this(capacity, new Guid[0])
		{
		}
		public DuplicateStore(int capacity, IEnumerable<Guid> identifiers)
		{
			this.capacity = capacity;
			this.entries = new HashSet<Guid>(new Guid[capacity]);

			identifiers = identifiers ?? new Guid[0];
			foreach (var identifier in identifiers)
				this.Add(identifier);
		}

		private readonly Queue<Guid> cache = new Queue<Guid>();
		private readonly HashSet<Guid> entries; 
		readonly int capacity;
	}
}