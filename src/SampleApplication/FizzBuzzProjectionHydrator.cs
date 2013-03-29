namespace SampleApplication
{
	using System.Collections.Generic;
	using System.Globalization;
	using Hydrospanner;

	public class FizzBuzzProjectionHydrator : 
		IHydratable, 
		IHydratable<CountEvent>, 
		IHydratable<FizzEvent>, 
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>
	{
		public const string TheKey = "/projections/fizzbuzz";

		public string Key { get { return TheKey; } }
		public bool IsComplete { get { return false; } }
		public bool IsPublicSnapshot { get { return true; } }
		
		public IEnumerable<object> GatherMessages()
		{
			yield break;
		}

		public object GetMemento()
		{
			return this.document;
		}

		public void Hydrate(CountEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Value = message.Value.ToString(CultureInfo.InvariantCulture);
		}

		public void Hydrate(FizzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Value = "Fizz";
		}

		public void Hydrate(BuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Value = "Buzz";
		}

		public void Hydrate(FizzBuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Value = "FizzBuzz";
		}

		public FizzBuzzProjectionHydrator(FizzBuzzProjection memento = null)
		{
			if (memento != null)
				this.document = memento;
		}

		public static FizzBuzzProjectionHydrator Create(FizzBuzzProjection memento)
		{
			return new FizzBuzzProjectionHydrator(memento);
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

		static readonly HydrationInfo Creation = new HydrationInfo(TheKey, () => new FizzBuzzProjectionHydrator());
		readonly FizzBuzzProjection document = new FizzBuzzProjection { Value = string.Empty };
	}
}