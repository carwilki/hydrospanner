namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;

	public sealed class TimeoutHydratable : IHydratable,
		IHydratable<CurrentTimeMessage>,
		IHydratable<TimeoutRequestedEvent>,
		IHydratable<TimeoutElapsedEvent>
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

		public void Hydrate(CurrentTimeMessage message, Dictionary<string, string> headers, bool live)
		{
			if (live)
				this.aggregate.Handle(message);
		}
		public void Hydrate(TimeoutRequestedEvent message, Dictionary<string, string> headers, bool live)
		{
			if (!live)
				this.aggregate.Apply(message);
		}
		public void Hydrate(TimeoutElapsedEvent message, Dictionary<string, string> headers, bool live)
		{
			if (live)
				this.aggregate.Apply(message);
		}
		public IEnumerable<object> GatherMessages()
		{
			var messages = this.aggregate.Messages;
			foreach (var message in messages)
				yield return message;

			messages.Clear();
		}

		public object GetMemento()
		{
			return this.aggregate.GetMemento();
		}
		public static TimeoutHydratable Restore(TimeoutMemento memento)
		{
			return new TimeoutHydratable(memento);
		}

		public static HydrationInfo Lookup(CurrentTimeMessage message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(TimeoutRequestedEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}
		public static HydrationInfo Lookup(TimeoutElapsedEvent message, Dictionary<string, string> headers)
		{
			return new HydrationInfo(HydratableKey, () => new TimeoutHydratable());
		}

		public TimeoutHydratable()
		{
			this.aggregate = new TimeoutAggregate();
		}
		public TimeoutHydratable(TimeoutMemento memento)
		{
			this.aggregate = new TimeoutAggregate(memento);
		}

		private const string HydratableKey = "/internal/timeout";
		private readonly TimeoutAggregate aggregate;
	}
}