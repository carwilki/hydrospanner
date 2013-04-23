namespace Hydrospanner.Timeout
{
	using System;

	public sealed class TimeoutMessage
	{
		public string Key { get; private set; }
		public DateTime Timeout { get; private set; }
		public DateTime UtcNow { get; private set; }

		public TimeoutMessage(string key, DateTime timeout, DateTime utcNow)
		{
			this.Key = key;
			this.Timeout = timeout;
			this.UtcNow = utcNow;
		}
	}
}