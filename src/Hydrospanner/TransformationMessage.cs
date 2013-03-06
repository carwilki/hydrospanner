namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public sealed class TransformationMessage
	{
		public long MessageSequence { get; set; }
		public bool Replay { get; set; } // have we ever seen and handled this message before?

		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }

		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public IHydratable[] Hydratables { get; set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.Replay = false;
			this.SerializedBody = null;
			this.SerializedHeaders = null;
			this.Body = null;
			this.Headers = null;
			this.Hydratables = null;
		}
	}
}