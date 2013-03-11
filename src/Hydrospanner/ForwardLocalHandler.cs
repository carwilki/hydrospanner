namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public sealed class ForwardLocalHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (!data.LiveMessage)
				return;

			this.BatchPublish(data);
			this.buffer.Enqueue(data); // avoid Disruptor ring buffer deadlocks by enqueuing locally and then attempting to forward if a slot is available
			this.CopyLocal();
		}

		private void BatchPublish(WireMessage data)
		{
			var wireMessage = data.AcknowledgeDelivery != null;
			var gathered = data.DispatchMessages;
			var batchSize = gathered.Count + (wireMessage ? 1 : 0);

			var batch = this.dispatch.NewBatchDescriptor(batchSize);
			batch = this.dispatch.Next(batch);

			if (wireMessage)
			{
				var target = this.dispatch[batch.Start];
				target.Clear();
				target.MessageSequence = data.MessageSequence;
				target.Body = data.Body;
				target.Headers = data.Headers;
				target.SerializedBody = data.SerializedBody;
				target.SerializedHeaders = data.SerializedHeaders;
				target.WireId = data.WireId;
				target.AcknowledgeDelivery = data.AcknowledgeDelivery;
			}
			
			for (var i = 0; i < gathered.Count; i++)
			{
				var pending = gathered[i];
				var item = this.dispatch[batch.Start + i + (wireMessage ? 1 : 0)];
				item.Clear();
				item.MessageSequence = data.MessageSequence + i + 1;
				item.Body = pending;
				item.Headers = new Dictionary<string, string>(); // TODO: where do these come from???
			}

			this.dispatch.Publish(batch);
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

		public ForwardLocalHandler(RingBuffer<WireMessage> wire, RingBuffer<DispatchMessage> dispatch)
		{
			this.wire = wire;
			this.dispatch = dispatch;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly Queue<WireMessage> buffer = new Queue<WireMessage>();
		private readonly RingBuffer<WireMessage> wire;
		private readonly RingBuffer<DispatchMessage> dispatch;
	}
}