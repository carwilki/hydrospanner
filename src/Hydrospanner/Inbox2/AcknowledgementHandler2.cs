namespace Hydrospanner.Inbox2
{
	using Disruptor;

	public class AcknowledgementHandler2 : IEventHandler<WireMessage2>
	{
		public void OnNext(WireMessage2 data, long sequence, bool endOfBatch)
		{
			if (endOfBatch)
				data.AcknowledgeDelivery();
		}
	}
}