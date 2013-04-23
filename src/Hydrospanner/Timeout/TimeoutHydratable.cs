namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable<TimeMessage>
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
		public object Memento
		{
			get { return null; }
		}

		public ITimeoutWatcher Watcher
		{
			get { return this.watcher; }
		}

		public void Hydrate(Delivery<TimeMessage> delivery)
		{
			this.watcher.Handle(delivery.Message);
		}

		public static HydrationInfo Lookup(Delivery<TimeMessage> delivery)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(Delivery<TimeoutMessage> delivery)
		{
			return new HydrationInfo(delivery.Message.Key, () => null); // used to route the the hydratable in question
		}

		public TimeoutHydratable()
		{
			this.watcher = new TimeoutWatcher(this.messages);
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly List<object> messages = new List<object>(); 
		private readonly TimeoutWatcher watcher;
	}
}