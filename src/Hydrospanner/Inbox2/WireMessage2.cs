namespace Hydrospanner.Inbox2
{
	using System;
	using System.Collections.Generic;

	public sealed class WireMessage2
	{
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public Guid StreamId { get; set; }
		public Guid WireId { get; set; }
		public bool LocalMessage { get; set; } // did this message originate here or was it received off the wire?

		public long SourceSequence { get; set; } // the incoming sequence from which this message originated (for bookmark/checkpoint)
		public long MessageSequence { get; set; } // the unique ID for this message

		public Action AcknowledgeDelivery { get; set; }

		public void Clear()
		{
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;

			this.StreamId = Guid.Empty;
			this.WireId = Guid.Empty;
			this.LocalMessage = false;

			this.SourceSequence = 0;
			this.MessageSequence = 0;

			this.AcknowledgeDelivery = null;
		}
	}
}