namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public class ForwardLocalHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}