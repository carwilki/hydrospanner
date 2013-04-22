namespace Hydrospanner
{
	using System.Collections.Generic;

	public struct Delivery<T>
	{
		public T Message
		{
			get { return this.message; }
		}
		public Dictionary<string, string> Headers
		{
			get { return this.headers; }
		}
		public long Sequence
		{
			get { return this.sequence; }
		}
		public bool Live
		{
			get { return this.live; }
		}

		public Delivery(T message, Dictionary<string, string> headers, long sequence, bool live) : this()
		{
			this.message = message;
			this.headers = headers ?? new Dictionary<string, string>();
			this.sequence = sequence;
			this.live = live;
		}

		private readonly T message;
		private readonly Dictionary<string, string> headers;
		private readonly long sequence;
		private readonly bool live;
	}
}