namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	internal class JournalItem
	{
		public long MessageSequence { get; private set; }

		public byte[] SerializedBody { get; private set; }
		public string SerializedType { get; private set; }
		public byte[] SerializedHeaders { get; private set; }
		public object Body { get; private set; }
		public Dictionary<string, string> Headers { get; private set; }

		public JournalItemAction ItemActions { get; private set; }
		public Guid ForeignId { get; private set; }
		public Action Acknowledgement { get; private set; }
	}

	[Flags]
	internal enum JournalItemAction
	{
		None = 0,
		Journal = 1,
		Dispatch = 2,
		Acknowledge = 4
	}
}