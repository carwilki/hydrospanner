namespace Hydrospanner.Phases.Journal
{
	using System.Collections.Generic;
	using Disruptor;
	using log4net;
	using Persistence;

	public sealed class JournalHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Received message sequence {0} of type {1} for journaling.", data.MessageSequence, data.SerializedType);
			if (data.ItemActions.HasFlag(JournalItemAction.Journal))
				this.buffer.Add(data);

			if (!endOfBatch || this.buffer.Count == 0)
				return;

			Log.DebugFormat("Flushing buffer with {0} items to journal.", this.buffer.Count);
			this.store.Save(this.buffer);

			Log.InfoFormat("{0} items committed to message journal.", this.buffer.Count);
			this.buffer.Clear();
		}

		public JournalHandler(IMessageStore store)
		{
			this.store = store;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(JournalHandler));
		private readonly List<JournalItem> buffer = new List<JournalItem>();
		private readonly IMessageStore store;
	}
}