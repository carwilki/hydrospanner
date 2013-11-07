namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;
	using Hydrospanner.Timeout;

	public class FizzBuzzAggregateHydrator : 
		IHydratable<CountCommand>,
		IHydratable<CountEvent>,
		IHydratable<FizzEvent>,
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>,
		IHydratable<TimeoutReachedEvent>
	{
		public string Key { get; private set; }
		public bool IsComplete { get { return this.aggregate.IsComplete; } }
		public bool IsPublicSnapshot { get { return false; } }
		public ICollection<object> PendingMessages
		{
			get { return this.aggregate.PendingMessages; }
		}
		public object Memento
		{
			get { return this.aggregate.Memento; }
		}
		public Type MementoType
		{
			get { return typeof(int); }
		}

		public void Hydrate(Delivery<CountCommand> delivery)
		{
			var message = delivery.Message;
			this.aggregate.Increment(message.StreamId, message.Value);
			if (delivery.Live)
				this.PendingMessages.Add(DateTime.UtcNow.AddSeconds(1));
		}
		public void Hydrate(Delivery<CountEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<FizzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<BuzzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<FizzBuzzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutReachedEvent> delivery)
		{
			if (delivery.Live)
				Console.WriteLine("Timeout Reached: " + delivery.Message.Instant);
		}

		public FizzBuzzAggregateHydrator(string key, int memento = 0)
		{
			this.Key = key;
			this.aggregate = new FizzBuzzAggregate(memento);
		}

		public static FizzBuzzAggregateHydrator Restore(string key, int memento)
		{
			return new FizzBuzzAggregateHydrator(key, memento);
		}

		public static HydrationInfo Lookup(Delivery<CountCommand> delivery)
		{
			return Lookup(delivery.Message.StreamId);
		}
		public static HydrationInfo Lookup(Delivery<CountEvent> delivery)
		{
			return Lookup(delivery.Message.StreamId);
		}
		public static HydrationInfo Lookup(Delivery<FizzEvent> delivery)
		{
			return Lookup(delivery.Message.StreamId);
		}
		public static HydrationInfo Lookup(Delivery<BuzzEvent> delivery)
		{
			return Lookup(delivery.Message.StreamId);
		}
		public static HydrationInfo Lookup(Delivery<FizzBuzzEvent> delivery)
		{
			return Lookup(delivery.Message.StreamId);
		}
		private static HydrationInfo Lookup(Guid streamId)
		{
			var key = string.Format(HydratableKeys.AggregateKey, streamId);
			return new HydrationInfo(key, () => new FizzBuzzAggregateHydrator(key));
		}

		private readonly FizzBuzzAggregate aggregate;
	}
}