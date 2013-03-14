﻿namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using Serialization;

	internal sealed class JournalItem
	{
		public long MessageSequence { get; set; }

		public byte[] SerializedBody { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public JournalItemAction ItemActions { get; set; }
		public Guid ForeignId { get; set; }
		public Action Acknowledgement { get; set; }

		public void AsForeignMessage(byte[] serializedBody, object body, Dictionary<string, string> headers, Guid foreignId, Action acknowledgement)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal;
			this.SerializedBody = serializedBody;
			this.Body = body;
			this.Headers = headers;
			this.ForeignId = foreignId;
			this.Acknowledgement = acknowledgement;
		}

		public void AsTransformationResultMessage(long sequence, object body, Dictionary<string, string> headers)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal;
			this.MessageSequence = sequence;
			this.Body = body;
			this.Headers = headers;
		}

		public void AsBootstrappedDispatchMessage(long sequence, byte[] body, string typeName, byte[] headers, Guid foreignId)
		{
			this.Clear();
			this.ItemActions = JournalItemAction.Dispatch;
			this.MessageSequence = sequence;
			this.SerializedBody = body;
			this.SerializedType = typeName;
			this.SerializedHeaders = headers;
			this.ForeignId = foreignId;
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

		public void Serialize(ISerializer serializer)
		{
			if (this.SerializedBody == null)
			{
				this.SerializedBody = serializer.Serialize(this.Body);
				if (this.Body != null)
					this.SerializedType = this.Body.GetType().AssemblyQualifiedName;
			}

			// TODO: add a test for when the bootstrapper loads the item directly from disk and pushes to be dispatched.
			////if (this.ItemActions.HasFlag(JournalItemAction.Dispatch) && this.Headers == null)
			////	this.Headers = serializer.Deserialize<Dictionary<string, string>>(this.SerializedHeaders);

			if (this.SerializedHeaders == null)
				this.SerializedHeaders = serializer.Serialize(this.Headers);
		}
	}

	[Flags]
	internal enum JournalItemAction
	{
		None = 0,
		Journal = 1,
		Dispatch = 2,
		Acknowledge = 4,
	}
}