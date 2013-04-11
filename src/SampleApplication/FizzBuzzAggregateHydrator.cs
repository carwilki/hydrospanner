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
			var messages = this.aggregate.Messages;
			for (var i = 0; i < messages.Count; i++)
				yield return messages[i];

			messages.Clear();
		}
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
			// aggregate invariant would be violated because the apply from the messages would happen much later

			if (live)
				this.aggregate.Increment(message.Value);
		}
		public void Hydrate(CountEvent message, Dictionary<string, string> headers, bool live)
		{
			this.aggregate.Apply(message);
		}
		public void Hydrate(FizzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.aggregate.Apply(message);
		}
		public void Hydrate(BuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.aggregate.Apply(message);
		}
		public void Hydrate(FizzBuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.aggregate.Apply(message);
		}

		public FizzBuzzAggregateHydrator(FizzBuzzAggregateMemento memento)
		{
			this.streamId = memento.StreamId;
			this.aggregate = new FizzBuzzAggregate(memento.StreamId, memento.Value);
		}
		public FizzBuzzAggregateHydrator(Guid streamId)
		{
			this.streamId = streamId;
			this.aggregate = new FizzBuzzAggregate(streamId);
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