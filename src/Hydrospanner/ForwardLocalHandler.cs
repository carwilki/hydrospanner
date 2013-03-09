namespace Hydrospanner
{
	using Disruptor;

	public class ForwardLocalHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.AcknowledgeDelivery != null)
				return; // this message has already been handled by the wire ring buffer

			var claimed = this.ring.Next();
			var item = this.ring[claimed];
			item.Clear();
			item.MessageSequence = data.MessageSequence;
			item.Body = data.Body;
			item.Headers = data.Headers;
			this.ring.Publish(claimed);
		}

		public ForwardLocalHandler(RingBuffer<WireMessage> ring)
		{
			this.ring = ring;
		}

		private readonly RingBuffer<WireMessage> ring;
	}
}