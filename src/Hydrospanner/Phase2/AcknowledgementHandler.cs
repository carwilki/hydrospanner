namespace Hydrospanner.Phase2
{
	using System;
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (data.IncomingWireMessage)
				this.confirmDelivery = data.ConfirmDelivery ?? this.confirmDelivery;

			if (endOfBatch)
				this.confirmDelivery();
		}

		private Action confirmDelivery;
	}
}