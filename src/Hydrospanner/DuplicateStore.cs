namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public class DuplicateStore
	{
		// TODO: At startup, populate this object instance X wire IDs all the way up to and including the checkpoint.
		// *DO NOT* include any wire IDs after the checkpoint because this will prevent those from ever being handled.

		public bool Contains(Guid key)
		{
			if (key == Guid.Empty)
				return false;

			if (this.hash.Contains(key))
				return true;

			this.index = (this.index + 1) % this.capacity;
			this.hash.Remove(this.window[this.index]);
			this.window[this.index] = key;
			return false;
		}

		public DuplicateStore(int capacity)
		{
			if (this.capacity <= 0 || capacity > (int.MaxValue - 16))
				throw new ArgumentOutOfRangeException("capacity");

			this.window = new Guid[capacity];
			this.capacity = capacity;
		}

		private readonly HashSet<Guid> hash = new HashSet<Guid>();
		private readonly Guid[] window;
		private readonly int capacity;
		private int index;
	}
}