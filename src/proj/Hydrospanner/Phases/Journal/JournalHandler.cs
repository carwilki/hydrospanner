namespace Hydrospanner.Phases.Journal
{
	using Disruptor;

	public sealed class JournalHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
		}
	}
}