namespace Hydrospanner.Inbox
{
	using System;
	using System.Collections;

	public sealed class WireMessage
	{
		public byte[] Payload { get; set; }
		public object Body { get; set; }
		public Hashtable Headers { get; set; }
		public Guid StreamId { get; set; }
		public long IncomingSequence { get; set; }
		public Action ConfirmDelivery { get; set; }

		public void Clear()
		{
			this.Payload = null;
			this.Headers = null;
			this.Body = null;
			this.StreamId = Guid.Empty;
			this.ConfirmDelivery = null;
			this.IncomingSequence = 0;
		}
	}
}