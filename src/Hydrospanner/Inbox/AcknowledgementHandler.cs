namespace Hydrospanner.Inbox
{
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (endOfBatch)
				data.ConfirmDelivery();
		}
	}
}