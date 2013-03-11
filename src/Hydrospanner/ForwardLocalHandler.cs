namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public sealed class ForwardLocalHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.DispatchOnly || data.AcknowledgeDelivery != null)
				return;

			this.buffer.Enqueue(data); // avoid Disruptor ring buffer deadlocks by enqueuing locally and then attempting to forward if a slot is available
			this.CopyLocal();
		}
		private void CopyLocal()
		{
			while (this.buffer.Count > 0)
			{
				var claimed = this.wire.Next(Timeout); // deadlocks can occur if the DB is down for a long time and the WireMessage ring buffer fills up.
				if (claimed == 0)
					return;

				var message = this.buffer.Dequeue();
				var item = this.wire[claimed];
				item.Clear();

				item.MessageSequence = message.MessageSequence;
				item.Body = message.Body;
				item.Headers = message.Headers;
				item.LiveMessage = true;

				this.wire.Publish(claimed);
			}
		}

		public ForwardLocalHandler(RingBuffer<WireMessage> wire)
		{
			this.wire = wire;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly Queue<DispatchMessage> buffer = new Queue<DispatchMessage>();
		private readonly RingBuffer<WireMessage> wire;
	}
}