namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner.Serialization;

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
			this.Clear();
			this.CanJournal = true;
			this.SerializedBody = body;
			this.SerializedType = type;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgement = ack;
		}

		public void AsLocalMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			this.Clear();
			this.IsLocal = true;
			this.MessageSequence = sequence;
			this.SerializedType = body.GetType().AssemblyQualifiedName;
			this.Body = body;
			this.Headers = headers;
		}

		public void AsJournaledMessage(long sequence, byte[] body, string type, byte[] headers, Guid foreignId)
		{
			this.Clear();
			this.MessageSequence = sequence;
			this.SerializedBody = body;
			this.SerializedType = type;
			this.SerializedHeaders = headers;
			this.IsDocumented = true;
			this.IsLocal = true;
			this.ForeignId = foreignId;
		}

		private void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = this.SerializedHeaders = null;
			this.Body = this.Headers = null;
			this.SerializedType = null;
			this.CanJournal = this.IsDocumented = this.IsLocal = this.IsDuplicate = false;
			this.ForeignId = Guid.Empty;
			this.Acknowledgement = null;
		}

		public void Deserialize(JsonSerializer serializer)
		{
			if (this.Body == null)
				this.Body = serializer.Deserialize(this.SerializedBody, this.SerializedType);

			if (this.Headers == null)
				this.Headers = serializer.Deserialize<Dictionary<string, string>>(this.SerializedHeaders);
		}
	}
}