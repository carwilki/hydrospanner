namespace Hydrospanner.Phases.Journal
{
	using System;
	using Disruptor;
	using log4net;

	public sealed class AcknowledgmentHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Receiving acknowledgement action for journal item of type {0}", data.SerializedType);

			this.ack = data.Acknowledgment ?? this.ack;

			if (!endOfBatch)
				return;

			if (this.ack == null)
				return;

			Log.Debug("Executing end-of-batch acknowledgement action.");

			this.ack();
			this.ack = null;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(AcknowledgmentHandler));
		private Action ack;
	}
}