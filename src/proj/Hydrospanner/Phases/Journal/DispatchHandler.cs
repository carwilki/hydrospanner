namespace Hydrospanner.Phases.Journal
{
	using System.Collections.Generic;
	using Disruptor;
	using Hydrospanner.Messaging;

	public sealed class DispatchHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			if (data.ItemActions.HasFlag(JournalItemAction.Dispatch))
				this.buffer.Add(data);

			if (!endOfBatch)
				return;

			while (true)
				if (this.Dispatch())
					break;

			this.buffer.Clear();
		}
		public bool Dispatch()
		{
			if (this.buffer.Count == 0)
				return true;

			for (var i = 0; i < this.buffer.Count; i++)
				if (!this.sender.Send(this.buffer[i]))
					return false;
			
			return this.sender.Commit();
		}

		public DispatchHandler(IMessageSender sender)
		{
			this.sender = sender;
		}

		private readonly List<JournalItem> buffer = new List<JournalItem>(1024 * 32);
		private readonly IMessageSender sender;
	}
}