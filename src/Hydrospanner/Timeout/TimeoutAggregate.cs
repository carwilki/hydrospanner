namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public class TimeoutAggregate
	{
		public void Handle(CurrentTimeMessage message)
		{
			foreach (var item in this.timeouts)
			{
				if (item.Key > message.UtcNow)
					break;

				foreach (var value in item.Value)
					this.Append(this.Apply, new TimeoutMessage(value, item.Key, message.UtcNow));
			}
		}

		private void Append<T>(Action<T> callback, T message)
		{
			this.pendingMessages.Add(message);
			callback(message);
		}

		public void Apply(TimeoutMessage message)
		{
			List<string> items;
			if (!this.timeouts.TryGetValue(message.Timeout, out items))
				return;

			this.count = this.count - items.RemoveAll(x => x == message.Key);
			if (items.Count == 0)
				this.timeouts.Remove(message.Timeout);
		}

		public TimeoutAggregate(List<object> pendingMessages)
		{
			this.pendingMessages = pendingMessages;
			this.timeouts = new SortedList<DateTime, List<string>>();
		}

		private readonly SortedList<DateTime, List<string>> timeouts;
		private readonly List<object> pendingMessages; 
		private int count;
	}
}