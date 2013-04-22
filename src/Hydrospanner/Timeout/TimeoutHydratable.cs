namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable,
		IHydratable<CurrentTimeMessage>,
		IHydratable<TimeoutRequestedEvent>,
		IHydratable<TimeoutElapsedEvent>
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

		public ICollection<object> PendingMessages { get; private set; }

		public void Hydrate(Delivery<CurrentTimeMessage> delivery)
		{
			this.aggregate.Handle(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutRequestedEvent> delivery)
		{
			if (!delivery.Live)
				this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutElapsedEvent> delivery)
		{
			if (delivery.Live)
				this.aggregate.Apply(delivery.Message);
		}

		public object GetMemento()
		{
			return this.aggregate.GetMemento();
		}
		public static TimeoutHydratable Restore(TimeoutMemento memento)
		{
			return new TimeoutHydratable(memento);
		}

		public static HydrationInfo Lookup(Delivery<CurrentTimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutRequestedEvent> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutElapsedEvent> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}

		public TimeoutHydratable()
		{
			this.aggregate = new TimeoutAggregate();
			this.PendingMessages = this.aggregate.PendingMessages;
		}
		public TimeoutHydratable(TimeoutMemento memento)
		{
			this.aggregate = new TimeoutAggregate(memento);
			this.PendingMessages = this.aggregate.PendingMessages;
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly TimeoutAggregate aggregate;
	}
}