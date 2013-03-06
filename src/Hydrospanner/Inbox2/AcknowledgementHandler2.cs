namespace Hydrospanner.Inbox2
{
	using System;
	using Disruptor;

	public class AcknowledgementHandler2 : IEventHandler<WireMessage2>
	{
		public void OnNext(WireMessage2 data, long sequence, bool endOfBatch)
		{
			this.ack = data.AcknowledgeDelivery ?? this.ack;

			if (endOfBatch && this.ack != null)
				this.ack();
		}

		private Action ack;
	}
}