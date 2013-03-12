namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class WireMessage
	{
		public long MessageSequence { get; set; } // the unique ID for this message
		public byte[] SerializedBody { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public bool WriteToJournal { get; set; } // indicates whether this message should be written to disk
		public bool LiveMessage { get; set; } // indicates whether this message has been seen before
		public bool DuplicateMessage { get; set; }
		public Guid WireId { get; set; } // used for de-duplication; indicates a message originated from an external source
		public Action AcknowledgeDelivery { get; set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedType = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;

			this.WriteToJournal = false;
			this.LiveMessage = false;
			this.DuplicateMessage = false;
			this.WireId = Guid.Empty;

			this.AcknowledgeDelivery = null;
		}
	}
}