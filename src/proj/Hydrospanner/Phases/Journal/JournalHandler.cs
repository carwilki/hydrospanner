namespace Hydrospanner.Phases.Journal
{
	using System;
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

			while (true)
			{
				if (this.storage.Save(this.buffer))
					break;

				StorageRetryTimeout.Sleep();
			}

			this.buffer.Clear();
		}

		public JournalHandler(IMessageStorage storage)
		{
			this.storage = storage;
		}

		private static readonly TimeSpan StorageRetryTimeout = TimeSpan.FromSeconds(5);
		private readonly List<JournalItem> buffer = new List<JournalItem>();
		private readonly IMessageStorage storage;
	}
}