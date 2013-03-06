namespace Hydrospanner.Inbox2
{
	using System;
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			this.ack = data.AcknowledgeDelivery ?? this.ack;

			if (endOfBatch && this.ack != null)
				this.ack();
		}

		private Action ack;
	}
}