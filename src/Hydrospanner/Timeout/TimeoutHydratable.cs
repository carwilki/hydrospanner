namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable,
		IHydratable<TimeoutRequestedEvent>,
		IHydratable<TimeoutElapsedEvent>,
		IHydratable<CurrentTimeMessage>
	{
		public string Key
		{
			get { return HydratableKey; }
		}
		public bool IsComplete
		{
			get { return false; }
		}
		public bool IsPublicSnapshot
		{
			get { return false; }
		}

		public IEnumerable<object> GatherMessages()
		{
			if (this.gathered.Count == 0)
				yield break;

			foreach (var item in this.gathered)
				yield return item;

			this.gathered.Clear();
		}

		public object GetMemento()
		{
			var list = new List<TimeoutEntry>(this.count);
			foreach (var item in this.timeouts)
				foreach (var value in item.Value)
					list.Add(new TimeoutEntry(value.Key, item.Key, value.Value));

			return new TimeoutMemento { Timeouts = list };
		}
		public static TimeoutHydratable Restore(TimeoutMemento memento)
		{
			return new TimeoutHydratable(memento.Timeouts);
		}

		public void Hydrate(TimeoutRequestedEvent message, Dictionary<string, string> headers, bool live)
		{
			if (live && message.Timeout <= DateTime.UtcNow)
				this.gathered.Add(message);
			else
			{
				List<KeyValuePair<string, int>> items;
				if (!this.timeouts.TryGetValue(message.Timeout, out items))
					this.timeouts[message.Timeout] = items = new List<KeyValuePair<string, int>>();

				this.count++;
				items.Add(new KeyValuePair<string, int>(message.Key, message.State));
			}
		}
		public void Hydrate(TimeoutElapsedEvent message, Dictionary<string, string> headers, bool live)
		{
			List<KeyValuePair<string, int>> items;
			if (!this.timeouts.TryGetValue(message.Timeout, out items))
				return;

			this.count = this.count - items.RemoveAll(x => x.Key == message.Key && x.Value == message.State);
			if (items.Count == 0)
				this.timeouts.Remove(message.Timeout);
		}
		public void Hydrate(CurrentTimeMessage message, Dictionary<string, string> headers, bool live)
		{
			foreach (var item in this.timeouts)
			{
				if (item.Key > message.UtcNow)
					break;

				foreach (var value in item.Value)
					this.gathered.Add(new TimeoutElapsedEvent(value.Key, item.Key, value.Value));
			}
		}
		public static HydrationInfo Lookup(TimeoutRequestedEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(TimeoutElapsedEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(CurrentTimeMessage message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}

		public TimeoutHydratable() : this(new List<TimeoutEntry>())
		{
		}
		public TimeoutHydratable(ICollection<TimeoutEntry> timeouts)
		{
			timeouts = timeouts ?? new TimeoutEntry[0];
			this.timeouts = new SortedList<DateTime, List<KeyValuePair<string, int>>>(timeouts.Count);

			foreach (var item in timeouts)
			{
				List<KeyValuePair<string, int>> items;
				if (!this.timeouts.TryGetValue(item.Timeout, out items))
					this.timeouts[item.Timeout] = items = new List<KeyValuePair<string, int>>();

				this.count++;
				items.Add(new KeyValuePair<string, int>(item.Key, item.State));
			}
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly List<object> gathered = new List<object>();
		private readonly SortedList<DateTime, List<KeyValuePair<string, int>>> timeouts;
		private int count;
	}
}