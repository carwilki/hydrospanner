namespace Hydrospanner.Phases.Journal
{
	using Disruptor;
	using Hydrospanner.Persistence;

	public sealed class DispatchCheckpointHandler : IEventHandler<JournalItem>
	{
		// as soon as the dispatched messages are published and committed on the broker
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			if (data.MessageSequence > this.saved)
			{
				this.save = true;
				this.saved = data.MessageSequence;
			}

			if (endOfBatch && this.save)
				this.storage.Save(this.saved);

			this.save = false;
		}

		public DispatchCheckpointHandler(IDispatchCheckpointStorage storage)
		{
			this.storage = storage;
		}

		private readonly IDispatchCheckpointStorage storage;
		private long saved;
		private bool save;
	}
}