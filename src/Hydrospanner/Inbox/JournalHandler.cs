namespace Hydrospanner.Inbox
{
	using Disruptor;

	public class JournalHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// TODO: callback into application code to determine the stream of a given message
			// at end of batch, write incoming message bytes to disk
		}
	}
}