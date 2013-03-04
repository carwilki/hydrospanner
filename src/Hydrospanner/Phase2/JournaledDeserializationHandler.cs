namespace Hydrospanner.Phase2
{
	using Disruptor;

	public class JournaledDeserializationHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (data.IncomingWireMessage)
				return;

			// TODO: deserialize the message
		}
	}
}