namespace Hydrospanner.Timeout
{
	using System;

	public class CurrentTimeMessage : IInternalMessage
	{
		public DateTime UtcNow { get; private set; }
 
		public CurrentTimeMessage(DateTime instant)
		{
			this.UtcNow = instant;
		}
	}
}