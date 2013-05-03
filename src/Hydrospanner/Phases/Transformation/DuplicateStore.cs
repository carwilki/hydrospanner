namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;

	public sealed class DuplicateStore
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
			this.cache = new Queue<Guid>(capacity);
			this.entries = new HashSet<Guid>(new Guid[capacity]);
			this.entries.Clear();

			identifiers = identifiers ?? new Guid[0];
			foreach (var identifier in identifiers)
				this.Add(identifier);
		}

		private readonly HashSet<Guid> entries;
		private readonly Queue<Guid> cache;
		readonly int capacity;
	}
}