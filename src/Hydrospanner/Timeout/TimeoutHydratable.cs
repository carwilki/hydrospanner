namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable, IHydratable<CurrentTimeMessage>
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

		public void Hydrate(Delivery<CurrentTimeMessage> delivery)
		{
			this.aggregate.Handle(delivery.Message);
		}

		public static HydrationInfo Lookup(Delivery<CurrentTimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, () => null);
		}

		private TimeoutHydratable()
		{
			this.aggregate = new TimeoutAggregate(this.messages);
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly List<object> messages = new List<object>(); 
		private readonly TimeoutAggregate aggregate;
	}
}