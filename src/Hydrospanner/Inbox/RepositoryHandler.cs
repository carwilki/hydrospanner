namespace Hydrospanner.Inbox
{
	using Disruptor;

	public class RepositoryHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// if !endOfBatch, add to list
			// at end of batch, for any stream that isn't in memory, create a set of hydratables and load the entire stream
			// push set of hydratables along with loaded stream (if any) to next phase
			// push each wire message and set of hydratables to next phase

			// callback into application code:
			// 1. ignore vs handle (e.g. it's a command and we've already handled this message before)
			// 2. new hydratables

			// if incoming message should be ignored, drop the message by not pushing it to the next phase
			// and don't worry about newing up hydratables or loading up the event stream for the incoming message

			// TODO: figure out how snapshotting works here
		}
	}
}