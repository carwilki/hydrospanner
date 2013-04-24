namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;
	using log4net;

	public sealed class AcknowledgmentHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Receiving acknowledgement action for journal item of type {0}, at sequence {1}", data.SerializedType, data.MessageSequence);

			this.ack = data.Acknowledgment ?? this.ack;

			if (!endOfBatch)
				return;

			if (this.ack == null)
				return;

			Log.InfoFormat("Executing end-of-batch acknowledgement action (current message sequence: {0}).", data.MessageSequence);

			this.ack(true);
			this.ack = null;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(AcknowledgmentHandler));
		private Action<bool> ack;
	}
}