namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;

	public class FizzBuzzProjectionHydrator :
		IHydratable,
		IHydratable<CountEvent>,
		IHydratable<FizzEvent>,
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>
	{
		public string Key { get { return KeyFactory(this.document.StreamId); } }
		public bool IsComplete { get { return false; } }
		public bool IsPublicSnapshot { get { return true; } }
		public IEnumerable<object> GatherMessages()
		{
			return NoMessages;
		}
		public object GetMemento()
		{
			return this.document;
		}

		public void Hydrate(CountEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Message = string.Empty;
			this.document.Value = message.Value;
		}
		public void Hydrate(FizzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Message = "Fizz";
			this.document.Value = message.Value;
		}
		public void Hydrate(BuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Message = "Buzz";
			this.document.Value = message.Value;
		}
		public void Hydrate(FizzBuzzEvent message, Dictionary<string, string> headers, bool live)
		{
			this.document.Message = "FizzBuzz";
			this.document.Value = message.Value;
		}

		public FizzBuzzProjectionHydrator(FizzBuzzProjection memento)
		{
			if (memento != null)
				this.document = memento;
		}
		public FizzBuzzProjectionHydrator(Guid streamId)
		{
			this.document = new FizzBuzzProjection
			{
				StreamId = streamId,
				Message = string.Empty,
				Value = 0
			};
		}

		public static FizzBuzzProjectionHydrator Restore(FizzBuzzProjection memento)
		{
			return new FizzBuzzProjectionHydrator(memento);
		}
		public static HydrationInfo Lookup(CountEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzProjectionHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(FizzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzProjectionHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(BuzzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzProjectionHydrator(message.StreamId));
		}
		public static HydrationInfo Lookup(FizzBuzzEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(KeyFactory(message.StreamId), () => new FizzBuzzProjectionHydrator(message.StreamId));
		}

		private static string KeyFactory(Guid streamId)
		{
			return string.Format(HydratableKeys.ProjectionKey, streamId);
		}

		private static readonly object[] NoMessages = new object[0];
		private readonly FizzBuzzProjection document;
	}
}