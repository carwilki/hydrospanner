namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;

	public sealed class AcknowledgmentHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			this.ack = data.Acknowledgment ?? this.ack;

			if (!endOfBatch)
				return;

			if (this.ack == null)
				return;

			this.ack();
			this.ack = null;
		}

		private Action ack;
	}
}