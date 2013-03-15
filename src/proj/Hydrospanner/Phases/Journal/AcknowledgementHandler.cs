namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<JournalItem>
	{
		// as soon as the message is journaled, e.g. concurrent with dispatch
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			this.ack = data.Acknowledgement ?? this.ack;

			if (endOfBatch && this.ack != null)
				this.ack();
		}

		private Action ack;
	}
}