namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;

	public class FizzBuzzProjectionHydrator :
		IHydratable<CountEvent>,
		IHydratable<FizzEvent>,
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>
	{
		public string Key { get; private set; }
		public bool IsComplete { get; private set; }
		public bool IsPublicSnapshot { get { return true; } }
		public object Memento { get { return this.projection; } }
		public Type MementoType
		{
			get { return typeof(FizzBuzzProjection); }
		}
		public ICollection<object> PendingMessages { get; private set; }

		public void Hydrate(Delivery<CountEvent> delivery)
		{
			this.projection.Message = string.Empty;
			this.projection.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<FizzEvent> delivery)
		{
			this.projection.Message = "Fizz";
			this.projection.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<BuzzEvent> delivery)
		{
			this.projection.Message = "Buzz";
			this.projection.Value = delivery.Message.Value;
		}
		public void Hydrate(Delivery<FizzBuzzEvent> delivery)
		{
			this.IsComplete = true;
			this.projection.Message = "FizzBuzz";
			this.projection.Value = delivery.Message.Value;
		}

		public FizzBuzzProjectionHydrator(string key, FizzBuzzProjection memento = null)
		{
			this.PendingMessages = new object[0];
			this.Key = key;
			this.projection = memento ?? new FizzBuzzProjection();
		}

		public static FizzBuzzProjectionHydrator Restore(string key, FizzBuzzProjection memento)
		{
			return new FizzBuzzProjectionHydrator(key, memento);
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
			var key = string.Format(HydratableKeys.ProjectionKey, streamId);
			return new HydrationInfo(key, () => new FizzBuzzProjectionHydrator(key));
		}

		private readonly FizzBuzzProjection projection;
	}
}