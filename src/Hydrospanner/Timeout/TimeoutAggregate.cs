namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public class TimeoutAggregate : ICloneable
	{
		public void DispatchTimeouts(DateTime now)
		{
			var keys = this.timeouts.Keys;
			for (var i = 0; i < keys.Count; i++)
			{
				var instant = keys[i];
				if (instant > now)
					return;

				foreach (var hydratableKey in this.timeouts[instant])
					this.pending.Add(new TimeoutReachedEvent(hydratableKey, instant, now));
			}
		}
		public void AbortTimeouts(string key)
		{
			foreach (var timeout in this.timeouts)
				if (timeout.Value.Contains(key))
					this.pending.Add(new TimeoutAbortedEvent(key, timeout.Key));
		}

		public void Apply(TimeoutRequestedEvent message)
		{
			this.Add(message.Key, message.Instant);
		}
		public void Apply(TimeoutAbortedEvent message)
		{
			this.Remove(message.Key, message.Instant);
		}
		public void Apply(TimeoutReachedEvent message)
		{
			this.Remove(message.Key, message.Instant);
		}
		private void Add(string key, DateTime instant)
		{
			HashSet<string> keys;
			if (!this.timeouts.TryGetValue(instant, out keys))
				this.timeouts[instant] = keys = new HashSet<string>();

			keys.Add(key);
		}
		private void Remove(string key, DateTime instant)
		{
			HashSet<string> keys;
			if (!this.timeouts.TryGetValue(instant, out keys))
				return;

			keys.Remove(key);

			if (keys.Count == 0)
				this.timeouts.Remove(instant);
		}

		public void Restore(TimeoutMemento memento)
		{
			if (memento != null)
				memento.CopyTo(this.timeouts);
		}
		public object Clone()
		{
			return new TimeoutMemento(this.timeouts);
		}

		public TimeoutAggregate(List<object> pending)
		{
			this.pending = pending;
		}

		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> pending;
	}
}