namespace Hydrospanner.Timeout
{
	using System;

	public sealed class TimeoutElapsedEvent
	{
		public string Key { get; private set; }
		public DateTime Timeout { get; private set; }
		public int State { get; private set; }

		public TimeoutElapsedEvent(string key, DateTime timeout, int state)
		{
			this.Key = key;
			this.Timeout = timeout;
			this.State = state;
		}
	}
}