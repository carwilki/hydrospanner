namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Serialization;

	public sealed class TransformationItem
	{
		public long MessageSequence { get; set; }

		public byte[] SerializedBody { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public Guid ForeignId { get; set; }
		public Action Acknowledgment { get; set; }

		public void AsForeignMessage(byte[] body, string type, Dictionary<string, string> headers, Guid foreignId, Action ack)
		{
			this.Clear();
			this.SerializedBody = body;
			this.SerializedType = type;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgment = ack;
		}

		public void AsLocalMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			this.Clear();
			this.MessageSequence = sequence;
			this.SerializedType = body.ResolvableTypeName();
			this.Body = body;
			this.Headers = headers;
		}

		public void AsJournaledMessage(long sequence, byte[] body, string type, byte[] headers)
		{
			this.Clear();
			this.MessageSequence = sequence;
			this.SerializedBody = body;
			this.SerializedType = type;
			this.SerializedHeaders = headers;
		}

		private void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = this.SerializedHeaders = null;
			this.Body = this.Headers = null;
			this.SerializedType = null;
			this.ForeignId = Guid.Empty;
			this.Acknowledgment = null;
		}

		public void Deserialize(ISerializer serializer)
		{
			if (this.Body == null)
				this.Body = serializer.Deserialize(this.SerializedBody, this.SerializedType);

			if (this.Headers == null)
				this.Headers = serializer.Deserialize<Dictionary<string, string>>(this.SerializedHeaders);
		}
	}
}