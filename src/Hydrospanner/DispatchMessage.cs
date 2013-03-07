namespace Hydrospanner
{
	using System.Collections.Generic;

	public sealed class DispatchMessage
	{
		public long MessageSequence { get; set; }
		public byte[] SerializedBody { get; set; }
		public object Body { get; set; }
		public Dictionary<string, string> Headers { get; set; }

		public void Clear()
		{
			this.MessageSequence = 0;
			this.SerializedBody = null;
			this.Body = null;
			this.Headers = null;
		}
	}
}