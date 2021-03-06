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
			get { return this.aggregate.Clone(); }
		}
		public Type MementoType
		{
			get { return typeof(TimeoutMemento); }
		}

		public IHydratable Abort(IHydratable hydratable)
		{
			this.aggregate.AbortTimeouts(hydratable.Key);
			return this;
		}
		public object Filter(string key, object message)
		{
			if (message is DateTime)
				return new TimeoutRequestedEvent(key, RoundUpToNearestSecond((DateTime)message));

			return message;
		}
		private static DateTime RoundUpToNearestSecond(DateTime instant)
		{
			// http://stackoverflow.com/questions/7029353/c-sharp-round-up-time-to-nearest-x-minutes
			return new DateTime(((instant.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond);
		}

		public void Hydrate(Delivery<CurrentTimeMessage> delivery)
		{
			this.aggregate.DispatchTimeouts(delivery.Message.UtcNow);
		}
		public void Hydrate(Delivery<TimeoutRequestedEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutAbortedEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutReachedEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
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

		public static TimeoutHydratable Restore(string key, TimeoutMemento memento)
		{
			var hydratable = new TimeoutHydratable();
			hydratable.aggregate.Restore(memento);
			return hydratable;
		}
		public static TimeoutHydratable Load(IRepository repository)
		{
			var message = new CurrentTimeMessage(DateTime.MinValue);
			var delivery = new Delivery<CurrentTimeMessage>(message, null, 0, false, true);
			return (TimeoutHydratable)repository.Load(delivery).Single();
		}
		internal TimeoutHydratable()
		{
			this.aggregate = new TimeoutAggregate(this.messages);
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly List<object> messages = new List<object>();
		private readonly TimeoutAggregate aggregate;
	}

	internal sealed class TimeoutReachedHydratableRoute
	{
		public static HydrationInfo Lookup(Delivery<TimeoutReachedEvent> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, Create);
		}
		private static IHydratable Create()
		{
#if DEBUG
			// this should only occur if the tombstome/graveyard window has passed/been exceeded.
			throw new InvalidOperationException("Default timeout route should never create a hydratable.");
#else
			return null;
#endif
		}
	}
}