namespace Hydrospanner
{
	using System;
	using Disruptor;

	public class ForwardToDispatchHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.WireId != Guid.Empty)
				return; // don't send anything that came off the wire

			var claimed = this.ring.Next();
			var message = this.ring[claimed];
			message.Clear();

			message.MessageSequence = data.MessageSequence;
			message.SerializedBody = data.SerializedBody;
			message.Body = data.Body;
			message.Headers = data.Headers;
			
			this.ring.Publish(sequence);
		}
		
		public ForwardToDispatchHandler(RingBuffer<DispatchMessage> ring)
		{
			this.ring = ring;
		}

		private readonly RingBuffer<DispatchMessage> ring;
	}
}