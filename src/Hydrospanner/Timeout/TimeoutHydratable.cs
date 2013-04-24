namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public sealed class TimeoutHydratable : IHydratable<TimeMessage>, ITimeoutWatcher
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
		public ICollection<object> PendingMessages
		{
			get { return this.messages; }
		}
		public object Memento
		{
			get { return null; }
		}

		public void Add(ITimeoutHydratable hydratable)
		{
			if (hydratable == null)
				return;

			var instants = hydratable.Timeouts;
			if (instants == null || instants.Count == 0)
				return;

			var key = hydratable.Key;
			foreach (var instant in instants)
			{
				var rounded = instant.AddMilliseconds(-instant.Millisecond);
				HashSet<string> keys;
				if (!this.timeouts.TryGetValue(rounded, out keys))
					this.timeouts[rounded] = keys = new HashSet<string>();

				keys.Add(key);
			}

			instants.Clear();
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
		public void Hydrate(Delivery<TimeMessage> delivery)
		{
			var message = delivery.Message;
			for (var i = 0; i < this.timeouts.Keys.Count; i++)
			{
				var instant = this.timeouts.Keys[i];
				if (instant > message.UtcNow)
					return;

				foreach (var hydratableKey in this.timeouts[instant])
					this.messages.Add(new TimeoutMessage(hydratableKey, instant, message.UtcNow));

				this.timeouts.RemoveAt(i);
			}
		}

		public static HydrationInfo Lookup(Delivery<TimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, () => null); // used to route the the hydratable in question
		}

		public static TimeoutHydratable Load(IRepository repository)
		{
			var message = new TimeMessage(DateTime.MinValue);
			var delivery = new Delivery<TimeMessage>(message, null, 0, true);

			var watcher = (TimeoutHydratable)repository.Load(delivery).Single();
			foreach (var hydratable in repository)
				watcher.Add(hydratable as ITimeoutHydratable);

			return watcher;
		}
		private TimeoutHydratable()
		{
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> messages = new List<object>();
	}
}