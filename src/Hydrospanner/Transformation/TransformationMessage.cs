namespace Hydrospanner.Transformation
{
	using System;

	public sealed class TransformationMessage
	{
		public Guid StreamId { get; set; }

        public long StreamLength { get; set; }
		public long StreamIndex { get; set; }
		public long BookmarkIndex { get; set; } // stream index > bookmark index means that this is the first time handling this message

		public byte[] Payload { get; set; }
		public byte[] Headers { get; set; }

		public object Body { get; set; }
		public object Metadata { get; set; }

		public IHydratable[] Hydratables { get; set; }

		public long IncomingSequence { get; set; }

		public void Clear()
		{
			this.StreamId = Guid.Empty;
		    this.StreamLength = 0;
			this.StreamIndex = 0;
			this.BookmarkIndex = 0;
			this.Payload = null;
			this.Headers = null;
			this.Body = null;
			this.Metadata = null;
			this.Hydratables = null;
			this.IncomingSequence = 0;
		}
	}
}