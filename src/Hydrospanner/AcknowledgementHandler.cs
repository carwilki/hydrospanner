namespace Hydrospanner
{
	using System;
	using Disruptor;

	public sealed class AcknowledgementHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
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