namespace Hydrospanner.Timeout
{
	using System;

	public sealed class TimeoutRequestedEvent : IInternalMessage
	{
		public string Key { get; private set; }
		public DateTime Instant { get; private set; }

		public TimeoutRequestedEvent(string key, DateTime instant)
		{
			this.Key = key;
			this.Instant = instant;
		}
	}
}