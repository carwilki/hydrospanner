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
		public object Memento { get { return this.document; } }
		public ICollection<object> PendingMessages { get; private set; }

		public void Hydrate(Delivery<CountEvent> delivery)
		{
			this.document.Message = string.Empty;
			this.document.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<FizzEvent> delivery)
		{
			this.document.Message = "Fizz";
			this.document.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<BuzzEvent> delivery)
		{
			this.document.Message = "Buzz";
			this.document.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<FizzBuzzEvent> delivery)
		{
			this.document.Message = "FizzBuzz";
			this.document.Value = delivery.Message.Value;
		}

		public FizzBuzzProjectionHydrator(FizzBuzzProjection memento)
		{
			this.PendingMessages = NoMessages;
			if (memento != null)
				this.document = memento;
		}
		public FizzBuzzProjectionHydrator(Guid streamId)
		{
			this.PendingMessages = NoMessages;
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