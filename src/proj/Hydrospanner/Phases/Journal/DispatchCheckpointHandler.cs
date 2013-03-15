namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public class DispatchCheckpointHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}