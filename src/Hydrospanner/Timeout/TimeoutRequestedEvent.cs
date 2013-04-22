namespace Hydrospanner.Timeout
{
	using System;

	public sealed class TimeoutRequestedEvent
	{
		public string Key { get; private set; }
		public DateTime Timeout { get; private set; }

		public TimeoutRequestedEvent(string key, DateTime timeout)
		{
			this.Key = key;
			this.Timeout = timeout;
		}
	}
}