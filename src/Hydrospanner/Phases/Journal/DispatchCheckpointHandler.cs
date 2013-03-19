namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;
	using Hydrospanner.Persistence;

	public sealed class DispatchCheckpointHandler : IEventHandler<JournalItem>
	{
		// as soon as the dispatched messages are published and committed on the broker
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			this.current = Math.Max(data.MessageSequence, this.current);

			if (!endOfBatch)
				return;

			if (this.current > this.previous)
				this.store.Save(this.current);

			this.previous = this.current;
		}

		public DispatchCheckpointHandler(IDispatchCheckpointStore store)
		{
			this.store = store;
		}

		private readonly IDispatchCheckpointStore store;
		private long previous;
		private long current;
	}
}