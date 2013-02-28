namespace Hydrospanner
{
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<ReceivedMessage>
	{
		public void OnNext(ReceivedMessage data, long sequence, bool endOfBatch)
		{
			if (endOfBatch)
				data.ConfirmDelivery();
		}
	}
}