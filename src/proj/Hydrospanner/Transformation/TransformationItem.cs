namespace Hydrospanner.Transformation
{
	using System;
	using System.Collections.Generic;

	public sealed class TransformationItem
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

		public void AsForeignMessage(byte[] body, string type, Dictionary<string, string> headers, Guid foreignId, Action ack)
		{
			// TODO: get this under test
			this.MessageSequence = 0;
			this.SerializedBody = body;
			this.SerializedType = type;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = headers;
			this.CanJournal = true;
			this.IsDocumented = false;
			this.IsLocal = false;
			this.IsDuplicate = false;
			this.ForeignId = foreignId;
			this.Acknowledgement = ack;
		}
		public void AsLocalMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			// TODO: get this under test
			this.MessageSequence = sequence;
			this.SerializedBody = null;
			this.SerializedType = body.GetType().AssemblyQualifiedName;
			this.SerializedHeaders = null;
			this.Body = body;
			this.Headers = headers;
			this.CanJournal = false;
			this.IsDocumented = false;
			this.IsLocal = true;
			this.IsDuplicate = false;
			this.ForeignId = Guid.Empty;
			this.Acknowledgement = null;
		}
	}
}