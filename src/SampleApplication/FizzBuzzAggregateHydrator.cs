namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;

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
		public IEnumerable<object> GatherMessages()
		{
			while (this.gathered.Count > 0)
				yield return this.gathered.Dequeue();
		}
		public object GetMemento()
		{
			return new KeyValuePair<Guid, int>(this.streamId, this.aggregate.Value);
		}

		public void Hydrate(CountCommand message, Dictionary<string, string> headers, bool live)
		{
			if (live)
				this.gathered.Enqueue(this.aggregate.Increment(message.Value));

			this.aggregate.Apply(message.Value);
		}
		public void Hydrate(CountEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message.Value);
		}
		public void Hydrate(FizzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message.Value);
		}
		public void Hydrate(BuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message.Value);
		}
		public void Hydrate(FizzBuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message.Value);
		}

		public FizzBuzzAggregateHydrator(KeyValuePair<Guid, int> memento)
		{
			this.streamId = memento.Key;
			this.aggregate = new FizzBuzzAggregate(memento.Value);
		}
		public FizzBuzzAggregateHydrator(Guid streamId)
		{
			this.streamId = streamId;
			this.aggregate = new FizzBuzzAggregate(streamId);
		}

		public static FizzBuzzAggregateHydrator Restore(KeyValuePair<Guid, int> memento)
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

		public static string KeyFactory(Guid streamId)
		{
			return string.Format(HydratableKeys.AggregateKey, streamId);
		}

		private readonly FizzBuzzAggregate aggregate;
		private readonly Queue<object> gathered = new Queue<object>();
		private readonly Guid streamId;
	}
}