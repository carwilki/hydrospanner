﻿namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public sealed class TimeoutHydratable : ITimeoutWatcher,
		IHydratable<CurrentTimeMessage>,
		IHydratable<TimeoutRequestedEvent>,
		IHydratable<TimeoutAbortedEvent>,
		IHydratable<TimeoutReachedEvent>
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
			get { return new TimeoutMemento(); }
		}

		public void Abort(IHydratable hydratable)
		{
			if (hydratable is IHydratable<TimeoutReachedEvent>)
				foreach (var timeout in this.timeouts)
					if (timeout.Value.Contains(hydratable.Key))
						this.PendingMessages.Add(new TimeoutAbortedEvent(hydratable.Key, timeout.Key));
		}

		public void Hydrate(Delivery<CurrentTimeMessage> delivery)
		{
			var message = delivery.Message;
			for (var i = 0; i < this.timeouts.Keys.Count; i++)
			{
				var instant = this.timeouts.Keys[i];
				if (instant > message.UtcNow)
					return;

				foreach (var hydratableKey in this.timeouts[instant])
					this.messages.Add(new TimeoutReachedEvent(hydratableKey, instant, message.UtcNow));

				this.timeouts.RemoveAt(i);
			}
		}
		public void Hydrate(Delivery<TimeoutRequestedEvent> delivery)
		{
			var message = delivery.Message;
			this.Add(message.Key, message.Instant);
		}
		public void Hydrate(Delivery<TimeoutAbortedEvent> delivery)
		{
			var message = delivery.Message;
			this.Remove(message.Key, message.Instant);
		}
		public void Hydrate(Delivery<TimeoutReachedEvent> delivery)
		{
			var message = delivery.Message;
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

		public static HydrationInfo Lookup(Delivery<CurrentTimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutRequestedEvent> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutAbortedEvent> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutReachedEvent> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}

		public static TimeoutHydratable Load(IRepository repository)
		{
			var message = new CurrentTimeMessage(DateTime.MinValue);
			var delivery = new Delivery<CurrentTimeMessage>(message, null, 0, true);
			return (TimeoutHydratable)repository.Load(delivery).Single();
		}
		private TimeoutHydratable()
		{
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly SortedList<DateTime, HashSet<string>> timeouts = new SortedList<DateTime, HashSet<string>>();
		private readonly List<object> messages = new List<object>();
	}

	internal sealed class TimeoutReachedHydratableRoute
	{
		public static HydrationInfo Lookup(Delivery<TimeoutReachedEvent> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, () => null); // used to route the the hydratable in question
		}
	}

	public class TimeoutMemento
	{
		// HUGE TODO: THIS IS A BIG FAT TODO--timeouts must be persisted 
	}
}