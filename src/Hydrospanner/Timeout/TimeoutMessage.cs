﻿namespace Hydrospanner.Timeout
{
	using System;

	public sealed class TimeoutMessage : IInternalMessage
	{
		public string Key { get; private set; }
		public DateTime Instant { get; private set; }
		public DateTime UtcNow { get; private set; }

		public TimeoutMessage(string key, DateTime instant, DateTime utcNow)
		{
			this.Key = key;
			this.Instant = instant;
			this.UtcNow = utcNow;
		}
	}
}