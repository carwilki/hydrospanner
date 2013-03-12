namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	internal class TransformationItem
	{
		public long MessageSequence { get; private set; }

		public byte[] SerializedBody { get; private set; }
		public string SerializedType { get; private set; }
		public byte[] SerializedHeaders { get; private set; }
		public object Body { get; private set; }
		public Dictionary<string, string> Headers { get; private set; }

		public bool CanJournal { get; private set; }
		public bool IsDocumented { get; private set; }
		public bool IsLocal { get; private set; }
		public bool IsDuplicate { get; private set; }
		public Guid ForeignId { get; private set; }
		public Action Acknowledgement { get; private set; }
	}
}