namespace SampleApplication
{
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
		public const string TheKey = "/aggregates/fizzbuzz";

		public string Key { get { return TheKey; } }

		public bool IsComplete { get { return false; } }
		
		public bool IsPublicSnapshot { get { return false; } }
		
		public IEnumerable<object> GatherMessages()
		{
			while (this.gathered.Count > 0)
				yield return this.gathered.Dequeue();
		}

		public object GetMemento()
		{
			return this.aggregate.Value;
		}

		public void Hydrate(CountCommand message, Dictionary<string, string> headers, bool live)
		{
			if (live)
				this.gathered.Enqueue(this.aggregate.Increment(message.Value));
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

		public FizzBuzzAggregateHydrator(int memento = 0)
		{
			this.aggregate = new FizzBuzzAggregate(memento);
		}

		readonly FizzBuzzAggregate aggregate = new FizzBuzzAggregate();
		readonly Queue<object> gathered = new Queue<object>();

		public static FizzBuzzAggregateHydrator Create(int memento)
		{
			return new FizzBuzzAggregateHydrator(memento);
		}

		public static HydrationInfo Lookup(CountCommand message, Dictionary<string, string> headers)
		{
			return Creation;
		}
		public static HydrationInfo Lookup(CountEvent message, Dictionary<string, string> headers)
		{
			return Creation;
		}
		public static HydrationInfo Lookup(FizzEvent message, Dictionary<string, string> headers)
		{
			return Creation;
		}
		public static HydrationInfo Lookup(BuzzEvent message, Dictionary<string, string> headers)
		{
			return Creation;
		}
		public static HydrationInfo Lookup(FizzBuzzEvent message, Dictionary<string, string> headers)
		{
			return Creation;
		}
		private static readonly HydrationInfo Creation = new HydrationInfo(TheKey, () => new FizzBuzzAggregateHydrator());
	}
}