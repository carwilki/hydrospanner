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

		public static HydrationInfo Lookup(Delivery<TimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			// TODO: it needs to go to the hydratable as well...
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