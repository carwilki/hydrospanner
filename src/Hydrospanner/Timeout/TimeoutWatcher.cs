namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public sealed class TimeoutWatcher : ITimeoutWatcher
	{
		public void AddRange(string key, ICollection<DateTime> instants)
		{
			if (instants != null)
				foreach (var instant in instants)
					this.Add(key, instant);
		}
		private void Add(string key, DateTime instant)
		{
			HashSet<string> keys;
			if (!this.timeouts.TryGetValue(instant, out keys))
				this.timeouts[instant] = keys = new HashSet<string>();

			keys.Add(key);
		}

		public void Remove(string key)
		{
			for (var i = this.timeouts.Keys.Count - 1; i >= 0; i--)
			{
				var instant = this.timeouts.Keys[i];

				var keys = this.timeouts[instant];
				if (!keys.Remove(key))
					continue;

				if (keys.Count > 0)
					continue;

				this.timeouts.RemoveAt(i);
			}
		}

		public void Handle(TimeMessage message)
		{
			for (var i = 0; i < this.timeouts.Keys.Count; i++)
			{
				var instant = this.timeouts.Keys[i];
				if (instant > message.UtcNow)
					return;

				foreach (var hydratableKey in this.timeouts[instant])
					this.pending.Add(new TimeoutMessage(hydratableKey, instant, message.UtcNow));

				this.timeouts.RemoveAt(i);
			}
		}

		public TimeoutWatcher(List<object> pending)
		{
			this.pending = pending;
		}

		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> pending; 
	}
}