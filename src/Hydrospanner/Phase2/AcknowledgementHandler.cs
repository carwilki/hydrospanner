namespace Hydrospanner.Phase2
{
	using Disruptor;

	public class AcknowledgementHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (endOfBatch)
				data.ConfirmDelivery();
		}
	}
}