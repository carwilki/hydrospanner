namespace Hydrospanner.Phases.Journal
{
	using System.Collections.Generic;
	using Disruptor;
	using Hydrospanner.Persistence;

	public sealed class JournalHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			if (data.ItemActions.HasFlag(JournalItemAction.Journal))
				this.buffer.Add(data);

			if (!endOfBatch || this.buffer.Count == 0)
				return;

			this.store.Save(this.buffer);
			this.buffer.Clear();
		}

		public JournalHandler(IMessageStore store)
		{
			this.store = store;
		}

		private readonly List<JournalItem> buffer = new List<JournalItem>();
		private readonly IMessageStore store;
	}
}