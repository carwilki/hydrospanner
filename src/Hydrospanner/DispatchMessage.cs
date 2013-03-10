namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class DispatchMessage
	{
		public long MessageSequence { get; set; } // assigned by the journaler

		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public bool DispatchOnly { get; set; }
		public Guid WireId { get; set; } // journaled for later use with de-duplication
		public Action AcknowledgeDelivery { get; set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;
			this.WireId = Guid.Empty;
			this.DispatchOnly = false;
			this.AcknowledgeDelivery = null;
		}
	}
}