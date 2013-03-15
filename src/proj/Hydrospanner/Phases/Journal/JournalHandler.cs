namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public class JournalHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}