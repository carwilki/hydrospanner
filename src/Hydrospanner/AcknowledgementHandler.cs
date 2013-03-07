namespace Hydrospanner
{
	using System;
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			this.ack = data.AcknowledgeDelivery ?? this.ack;

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