namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public class TimeoutManager
	{
		public void Add(string key, ICollection<DateTime> instants)
		{
			if (instants == null || instants.Count == 0)
				return;

			foreach (var instant in instants)
				this.Add(key, instant);

			instants.Clear();
		}
		private void Add(string key, DateTime instant)
		{
			HashSet<string> keys;
			if (!this.timeouts.TryGetValue(instant, out keys))
				this.timeouts[instant] = keys = new HashSet<string>();

			keys.Add(key);
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

		public TimeoutManager(List<object> pending)
		{
			this.pending = pending;
		}

		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> pending; 
	}
}