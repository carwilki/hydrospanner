namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using log4net;
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
		public Action<bool> Acknowledgment { get; set; }

		public bool IsTransient { get; set; }

		public void AsForeignMessage(byte[] body, string type, Dictionary<string, string> headers, Guid foreignId, Action<bool> ack)
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
		public void AsTransientMessage(byte[] body, string type, Dictionary<string, string> headers, Guid foreignId, Action<bool> ack)
		{
			this.Clear();
			this.SerializedBody = body;
			this.SerializedType = type;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgment = ack;
			this.IsTransient = true;
		}
		public void AsTransientMessage(object body)
		{
			this.Clear();
			this.Body = body;
			this.IsTransient = true;
		}
		private void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = this.SerializedHeaders = null;
			this.Body = this.Headers = null;
			this.SerializedType = null;
			this.ForeignId = Guid.Empty;
			this.Acknowledgment = null;
			this.IsTransient = false;
		}

		public void Deserialize(ISerializer serializer)
		{
			try
			{
				if (this.Body == null)
					this.Body = serializer.Deserialize(this.SerializedBody, this.SerializedType);

				if (this.Headers == null)
					this.Headers = serializer.Deserialize<Dictionary<string, string>>(this.SerializedHeaders);
			}
			catch (SerializationException e)
			{
				this.Body = null;
				this.Headers = null;

				if (this.Acknowledgment != null)
				{
					Log.Warn("Unable to deserialize item of type '{0}'".FormatWith(this.SerializedType), e);
					this.Acknowledgment(false);
				}
				else
					Log.Fatal("Unable to deserialize item of type '{0}'".FormatWith(this.SerializedType), e);
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(TransformationItem));
	}
}