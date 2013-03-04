namespace Hydrospanner.Phase2
{
	using Disruptor;
	using Hydrospanner.Phase3;

	public class ReplicationHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (!data.IncomingWireMessage)
				return;

			var count = data.PendingDispatch.Count;
			var descriptor = this.nextPhase.NewBatchDescriptor(count);
			var claimed = this.nextPhase.Next(descriptor);

			for (var i = 0; i < count; i++)
			{
				var seq = i + claimed.Start;
				var message = this.nextPhase[seq];
				message.Body = data.PendingDispatch[i];
				message.ConfirmDelivery = data.ConfirmDelivery;
			}

			this.nextPhase.Publish(descriptor);
		}

		public ReplicationHandler(RingBuffer<DispatchMessage> nextPhase)
		{
			this.nextPhase = nextPhase;
		}

		private readonly RingBuffer<DispatchMessage> nextPhase;
	}
}