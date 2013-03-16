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
			// TODO: this is spike code to just get an idea of what this might look like
			// where all the SQL (storage stuff) is pushed into the storage class.
			if (data.ItemActions.HasFlag(JournalItemAction.Journal))
				this.buffer.Add(data);

			if (!endOfBatch)
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