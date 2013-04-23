namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class TimeoutAggregate
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
			foreach (var instant in this.timeouts)
			{
				if (instant.Key > message.UtcNow)
					break;

				foreach (var hydratableKey in instant.Value.ToArray())
					this.Append(this.Apply, new TimeoutMessage(hydratableKey, instant.Key, message.UtcNow));
			}
		}
		private void Append<T>(Action<T> callback, T message)
		{
			this.pending.Add(message);
			callback(message);
		}
		private void Apply(TimeoutMessage message)
		{
			HashSet<string> keys;
			if (!this.timeouts.TryGetValue(message.Instant, out keys))
				return;

			if (!keys.Remove(message.Key))
				return;

			if (keys.Count > 0)
				return;

			this.timeouts.Remove(message.Instant);
		}

		public TimeoutAggregate(List<object> pending)
		{
			this.pending = pending;
		}

		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> pending; 
	}
}