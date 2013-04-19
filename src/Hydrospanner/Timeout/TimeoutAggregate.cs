namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public class TimeoutAggregate
	{
		public List<object> Messages { get; private set; } 

		public void Handle(CurrentTimeMessage message)
		{
			foreach (var item in this.timeouts)
			{
				if (item.Key > message.UtcNow)
					break;

				foreach (var value in item.Value)
					this.Append(this.Apply, new TimeoutElapsedEvent(value.Key, item.Key, value.Value));
			}
		}
		private void Append<T>(Action<T> callback, T message)
		{
			this.Messages.Add(message);
			callback(message);
		}
		public void Apply(TimeoutRequestedEvent message)
		{
			List<KeyValuePair<string, int>> items;
			if (!this.timeouts.TryGetValue(message.Timeout, out items))
				this.timeouts[message.Timeout] = items = new List<KeyValuePair<string, int>>();

			this.count++;
			items.Add(new KeyValuePair<string, int>(message.Key, message.State));
		}
		public void Apply(TimeoutElapsedEvent message)
		{
			List<KeyValuePair<string, int>> items;
			if (!this.timeouts.TryGetValue(message.Timeout, out items))
				return;

			this.count = this.count - items.RemoveAll(x => x.Key == message.Key && x.Value == message.State);
			if (items.Count == 0)
				this.timeouts.Remove(message.Timeout);
		}

		public object GetMemento()
		{
			var list = new List<TimeoutEntry>(this.count);
			foreach (var item in this.timeouts)
				foreach (var value in item.Value)
					list.Add(new TimeoutEntry(value.Key, item.Key, value.Value));

			return new TimeoutMemento { Timeouts = list };
		}

		public TimeoutAggregate()
		{
			this.Messages = new List<object>();
		}
		public TimeoutAggregate(TimeoutMemento memento) : this()
		{
			this.timeouts = new SortedList<DateTime, List<KeyValuePair<string, int>>>(memento.Timeouts.Count);

			foreach (var item in memento.Timeouts)
			{
				List<KeyValuePair<string, int>> list;
				if (!this.timeouts.TryGetValue(item.Timeout, out list))
					this.timeouts[item.Timeout] = list = new List<KeyValuePair<string, int>>();

				this.count++;
				list.Add(new KeyValuePair<string, int>(item.Key, item.State));
			}
		}

		private readonly SortedList<DateTime, List<KeyValuePair<string, int>>> timeouts;
		private int count;
	}
}