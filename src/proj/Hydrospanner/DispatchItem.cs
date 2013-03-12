namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public class DispatchItem
	{
		public long MessageSequence { get; private set; }

		public byte[] SerializedBody { get; private set; }
		public string SerializedType { get; private set; }
		public byte[] SerializedHeaders { get; private set; }
		public object Body { get; private set; }
		public Dictionary<string, string> Headers { get; private set; }

		public bool DispatchOnly { get; private set; }
		public bool WriteToJournal { get; private set; }
		public Guid WireId { get; private set; }
		public Action AcknowledgeDelivery { get; private set; }
	}
}