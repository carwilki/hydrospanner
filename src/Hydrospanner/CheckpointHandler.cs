namespace Hydrospanner
{
	using Disruptor;

	public class CheckpointHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (endOfBatch)
				this.store.UpdateDispatchCheckpoint(data.MessageSequence);
		}

		public CheckpointHandler(MessageStore store)
		{
			this.store = store;
		}

		private readonly MessageStore store;
	}
}