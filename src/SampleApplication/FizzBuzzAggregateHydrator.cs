namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;
	using Hydrospanner.Timeout;

	public class FizzBuzzAggregateHydrator : 
		IHydratable, 
		IHydratable<CountCommand>,
		IHydratable<CountEvent>,
		IHydratable<FizzEvent>,
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>
	{
		public string Key { get { return KeyFactory(this.streamId); } }
		public bool IsComplete { get { return this.aggregate.IsComplete; } }
		public bool IsPublicSnapshot { get { return false; } }
		public ICollection<object> PendingMessages { get; private set; }
		public object GetMemento()
		{
			return new FizzBuzzAggregateMemento
			{
				StreamId = this.streamId,
				Value = this.aggregate.Value,
			};
		}

		public void Hydrate(CountCommand message, Dictionary<string, string> headers, bool live)
		{
			// invocation of a method *must* result in the aggregate transforming itself internally, otherwise
			// aggregate invariants would be violated because the apply from the messages would happen much later

			if (live)
				this.aggregate.Increment(message.Value);
		}
		public void Hydrate(CountEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message);
		}
		public void Hydrate(FizzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message);
		}
		public void Hydrate(BuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live) 
				this.aggregate.Apply(message);
		}
		public void Hydrate(FizzBuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live) 
				this.aggregate.Apply(message);
		}

		public FizzBuzzAggregateHydrator(FizzBuzzAggregateMemento memento)
		{
			this.streamId = memento.StreamId;
			this.aggregate = new FizzBuzzAggregate(memento.StreamId, memento.Value);
			this.PendingMessages = new List<object>();
		}
		public FizzBuzzAggregateHydrator(Guid streamId)
		{
			this.streamId = streamId;
			this.aggregate = new FizzBuzzAggregate(streamId);
			this.PendingMessages = new List<object>();
		}

		public static FizzBuzzAggregateHydrator Restore(FizzBuzzAggregateMemento memento)
		{
			return new FizzBuzzAggregateHydrator(memento);
		}

		public static HydrationInfo Lookup(CountCommand message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzAggregateHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(CountEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzAggregateHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(FizzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzAggregateHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(BuzzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzAggregateHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(FizzBuzzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzAggregateHydrator(message.StreamId));
		}
		public static HydrationInfo Loookup(TimeoutElapsedEvent message, Dictionary<string, string> headers)
		{
			throw new NotImplementedException();
		}

		public static string KeyFactory(Guid streamId)
		{
			return string.Format(HydratableKeys.AggregateKey, streamId);
		}

		private readonly FizzBuzzAggregate aggregate;
		private readonly Guid streamId;
	}

	public class FizzBuzzAggregateMemento
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}
}