namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public class ForwardLocalHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.DispatchOnly)
				return; // instructed to not forward back to local ring buffer

			if (data.AcknowledgeDelivery != null)
				return; // this message has already been handled by the wire ring buffer

			this.buffer.Enqueue(data); // avoid Disruptor ring buffer deadlocks by enqueuing locally and then attempting to forward if a slot is available

			while (this.buffer.Count > 0)
			{
				var claimed = this.ring.Next(Timeout); // deadlocks can occur if the DB is down for a long time and the WireMessage ring buffer fills up.
				if (claimed == 0)
					return;

				var message = this.buffer.Dequeue();
				var item = this.ring[claimed];
				item.Clear();

				item.MessageSequence = message.MessageSequence;
				item.Body = message.Body;
				item.Headers = message.Headers;

				this.ring.Publish(claimed);
			}
		}

		public ForwardLocalHandler(RingBuffer<WireMessage> ring)
		{
			this.ring = ring;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly Queue<DispatchMessage> buffer = new Queue<DispatchMessage>();
		private readonly RingBuffer<WireMessage> ring;
	}
}