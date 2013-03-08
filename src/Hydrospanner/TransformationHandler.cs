namespace Hydrospanner
{
	using Disruptor;

	public class TransformationHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// all complexity 
		}

		public TransformationHandler(RingBuffer<DispatchMessage> dispatch, RingBuffer<SnapshotMessage> snapshot)
		{
			this.dispatch = dispatch;
			this.snapshot = snapshot;
		}

		private readonly RingBuffer<SnapshotMessage> snapshot;
		private readonly RingBuffer<DispatchMessage> dispatch;
	}
}