namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class DispatchMessage
	{
		public long MessageSequence { get; set; }

		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public Guid WireId { get; set; } // used for de-duplication; indicates a message originated from an external source
		public Action AcknowledgeDelivery { get; set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;
			this.WireId = Guid.Empty;
			this.AcknowledgeDelivery = null;
		}
	}
}