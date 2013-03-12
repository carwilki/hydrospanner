namespace Hydrospanner.Journal
{
	using System;
	using System.Collections.Generic;

	public class JournalItem
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

		public void AsForeignMessage(byte[] serializedBody, object body, Dictionary<string, string> headers, Guid foreignId, Action acknowledgement)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal;
			this.SerializedBody = serializedBody;
			this.SerializedType = body.GetType().AssemblyQualifiedName;
			this.Body = body;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgement = acknowledgement;
		}

		public void AsLocalMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal;
			this.MessageSequence = sequence;
			this.Body = body;
			this.SerializedType = body.GetType().AssemblyQualifiedName;
			this.Headers = headers;
		}

		private void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = this.SerializedHeaders = null;
			this.SerializedType = null;
			this.Body = null;
			this.Headers = null;
			this.ItemActions = JournalItemAction.None;
			this.ForeignId = Guid.Empty;
			this.Acknowledgement = null;
		}
	}

	[Flags]
	public enum JournalItemAction
	{
		None = 0,
		Journal = 1,
		Dispatch = 2,
		Acknowledge = 4,
	}
}