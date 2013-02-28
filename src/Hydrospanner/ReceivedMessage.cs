namespace Hydrospanner
{
	using System.Collections;

	public sealed class ReceivedMessage
	{
		public byte[] RawBody { get; set; }
		public object Body { get; set; }
		public Hashtable Headers { get; set; }
		public int ChannelId { get; set; }
		public ulong DeliveryTag { get; set; }

		public void Clear()
		{
			this.RawBody = null;
			this.Headers = null;
			this.Body = null;
			this.DeliveryTag = 0;
		}
	}
}