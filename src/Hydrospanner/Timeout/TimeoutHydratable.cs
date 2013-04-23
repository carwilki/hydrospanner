namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable, IHydratable<TimeMessage>
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
		public ICollection<object> PendingMessages
		{
			get { return this.messages; }
		}
		public object GetMemento()
		{
			return null;
		}

		public void Hydrate(Delivery<TimeMessage> delivery)
		{
			this.aggregate.Handle(delivery.Message);
		}
		public void Hydrate(Delivery<TimeoutMessage> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}

		public static HydrationInfo Lookup(Delivery<TimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}

		private TimeoutHydratable()
		{
			this.aggregate = new TimeoutAggregate(this.messages);
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly List<object> messages = new List<object>(); 
		private readonly TimeoutAggregate aggregate;
	}

	internal static class TimeoutRoutes
	{
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, () => null); // used to route the the hydratable in question
		}
	}
}