namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class WireMessage
	{
		public long MessageSequence { get; set; } // the unique ID for this message
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public bool LiveMessage { get; set; } // indicates whether this message has been seen before
		public bool DuplicateMessage { get; set; }
		public Guid WireId { get; set; } // used for de-duplication; indicates a message originated from an external source
		public Action AcknowledgeDelivery { get; set; }

		public List<object> DispatchMessages { get; private set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;

			this.DuplicateMessage = false;
			this.LiveMessage = false;
			this.WireId = Guid.Empty;

			this.AcknowledgeDelivery = null;
			this.DispatchMessages.Clear();
		}

		public WireMessage()
		{
			this.DispatchMessages = new List<object>();
		}
	}
}