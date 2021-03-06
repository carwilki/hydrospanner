﻿namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using Messaging;
	using Serialization;

	public sealed class JournalItem
	{
		public long MessageSequence { get; set; }

		public byte[] SerializedBody { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public JournalItemAction ItemActions { get; set; }
		public Guid ForeignId { get; set; }
		public Action<Acknowledgment> Acknowledgment { get; set; }

		public void AsForeignMessage(long sequence, byte[] serializedBody, object body, Dictionary<string, string> headers, Guid foreignId, Action<Acknowledgment> acknowledgment)
		{
			this.Clear();
			this.MessageSequence = sequence;
			this.ItemActions = JournalItemAction.Acknowledge | (sequence > 0 ? JournalItemAction.Journal : JournalItemAction.None);
			this.SerializedBody = serializedBody;
			this.Body = body;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgment = acknowledgment;

			if (sequence > 0)
				this.SerializedType = body.ResolvableTypeName();
		}
		public void AsTransformationResultMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal;
			this.MessageSequence = sequence;
			this.Body = body;
			this.Headers = headers;
			if (body == null)
				return;

			this.SerializedType = body.ResolvableTypeName();
			if (this.Body is IInternalMessage)
				this.ItemActions = JournalItemAction.Journal;
		}
		public void AsBootstrappedDispatchMessage(long sequence, byte[] body, string typeName, byte[] headers)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Dispatch;
			this.MessageSequence = sequence;
			this.SerializedBody = body;
			this.SerializedType = typeName;
			this.SerializedHeaders = headers;
		}
		public void Serialize(ISerializer serializer)
		{
			if (this.SerializedBody == null)
			{
				this.SerializedBody = serializer.Serialize(this.Body);
				if (this.Body != null)
					this.SerializedType = this.Body.ResolvableTypeName();

				this.Body = null;
			}

			if (this.ItemActions.HasFlag(JournalItemAction.Dispatch) && this.Headers == null)
			{
				this.Headers = serializer.Deserialize<Dictionary<string, string>>(this.SerializedHeaders);
				this.SerializedHeaders = null;
			}
			else if (this.SerializedHeaders == null && this.Headers != null && this.Headers.Count > 0)
			{
				this.SerializedHeaders = serializer.Serialize(this.Headers);
				this.Headers = null;
			}
		}
		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.SerializedType = null;
			this.Body = null;
			this.Headers = null;
			this.ItemActions = JournalItemAction.None;
			this.ForeignId = Guid.Empty;
			this.Acknowledgment = null;
		}
	}
}