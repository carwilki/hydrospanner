namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class WireMessage
	{
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public bool DuplicateMessage { get; set; }
		public Guid StreamId { get; set; }
		public Guid WireId { get; set; } // used for de-duplication; indicates a message originated from an external source

		public long SourceSequence { get; set; } // the sequence of the message from which *this* message originated (for checkpoint purposes)
		public long MessageSequence { get; set; } // the unique ID for this message

		public Action AcknowledgeDelivery { get; set; }

		public void Clear()
		{
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;

			this.DuplicateMessage = false;
			this.StreamId = Guid.Empty;
			this.WireId = Guid.Empty;

			this.SourceSequence = 0;
			this.MessageSequence = 0;

			this.AcknowledgeDelivery = null;
		}
	}
}