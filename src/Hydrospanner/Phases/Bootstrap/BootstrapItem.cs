namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Collections.Generic;

	public sealed class BootstrapItem
	{
		public Type MementoType { get; set; }
		public byte[] SerializedMemento { get; set; }

		public long MessageSequence { get; set; }
		public byte[] SerializedBody { get; set; }
		public string SerialziedMessageType { get; set; }
		public byte[] SerializedHeaders { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public void AsSnapshot(Type type, byte[] memento)
		{
			this.Clear();
			this.MementoType = type;
			this.SerializedMemento = memento;
		}

		public void AsReplayMessage(long sequence, string typeName, byte[] body, byte[] headers)
		{
			this.Clear();
			this.MessageSequence = sequence;
			this.SerialziedMessageType = typeName;
			this.SerializedBody = body;
			this.SerializedHeaders = headers;
		}

		private void Clear()
		{
			this.MementoType = null;
			this.SerializedMemento = null;
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.SerialziedMessageType = null;
			this.Body = null;
			this.Headers = null;
		}
	}
}