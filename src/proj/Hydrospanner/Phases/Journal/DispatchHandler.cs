namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public class DispatchHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}