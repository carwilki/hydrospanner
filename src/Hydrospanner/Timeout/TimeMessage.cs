namespace Hydrospanner.Timeout
{
	using System;

	public class TimeMessage
	{
		public DateTime UtcNow { get; private set; }
 
		public TimeMessage(DateTime instant)
		{
			this.UtcNow = instant;
		}
	}
}