namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;
	using log4net;
	using Messaging;

	public sealed class AcknowledgmentHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Receiving acknowledgement action for journal item of type {0}, at sequence {1}", data.SerializedType, data.MessageSequence);

			this.ack = data.Acknowledgment ?? this.ack;
			this.max = Math.Max(data.MessageSequence, this.max);

			if (!endOfBatch)
				return;

			if (this.ack == null)
				return;

			Log.DebugFormat("Executing end-of-batch acknowledgement action (current message sequence: {0}).", this.max);

			this.ack(Acknowledgment.ConfirmBatch);
			this.ack = null;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(AcknowledgmentHandler));
		private Action<Acknowledgment> ack;
		private long max;
	}
}