namespace Hydrospanner.Outbox
{
	using System.Collections;

	public sealed class DispatchMessage
	{
		public object Body { get; set; }
		public Hashtable Headers { get; set; }

		public long IncomingSequence { get; set; }
		public byte[] Payload { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}