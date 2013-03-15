namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public class DispatchCheckpointHandler : IEventHandler<JournalItem>
	{
		// as soon as the dispatched messages are published and committed on the broker
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}