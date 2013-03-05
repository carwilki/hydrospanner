namespace Hydrospanner.Inbox
{
	using System;
	using System.Collections;

	public sealed class WireMessage
	{
		public Guid WireId { get; set; }
		public byte[] Payload { get; set; }
		public object Body { get; set; }
		public Hashtable Headers { get; set; }
		public Guid StreamId { get; set; }
		public long IncomingSequence { get; set; }
		public Action ConfirmDelivery { get; set; }

		public void Clear()
		{
			this.WireId = Guid.Empty;
			this.Payload = null;
			this.Body = null;
			this.Headers = null;
			this.StreamId = Guid.Empty;
			this.IncomingSequence = 0;
			this.ConfirmDelivery = null;
		}
	}
}