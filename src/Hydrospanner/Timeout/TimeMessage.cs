namespace Hydrospanner.Timeout
{
	using System;

	public class TimeMessage : IInternalMessage
	{
		public DateTime UtcNow { get; private set; }
 
		public TimeMessage(DateTime instant)
		{
			this.UtcNow = instant;
		}
	}
}