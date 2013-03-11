namespace Hydrospanner
{
	using System;
	using Disruptor;

	public sealed class CheckpointHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			this.checkpoint = Math.Max(this.checkpoint, data.MessageSequence);

			if (endOfBatch)
				this.store.UpdateDispatchCheckpoint(this.checkpoint);
		}

		public CheckpointHandler(MessageStore store)
		{
			this.store = store;
		}

		private readonly MessageStore store;
		private long checkpoint;
	}
}