﻿namespace Hydrospanner.Timeout
{
	using System;
	using System.ComponentModel;

	[Description("json:underscore")]
	public sealed class TimeoutAbortedEvent : IInternalMessage
	{
		public string Key { get; private set; }
		public DateTime Instant { get; private set; }

		public TimeoutAbortedEvent(string key, DateTime instant)
		{
			this.Key = key;
			this.Instant = instant;
		}
	}
}