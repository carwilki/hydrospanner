namespace Hydrospanner
{
	using System;
	using System.Collections;

	public sealed class ReceivedMessage
	{
		public byte[] Payload { get; set; }
		public object Body { get; set; }
		public Hashtable Headers { get; set; }
		public Action ConfirmDelivery { get; set; }

		public void Clear()
		{
			this.Payload = null;
			this.Headers = null;
			this.Body = null;
			this.ConfirmDelivery = null;
		}
	}
}