namespace Hydrospanner.Phase3
{
	using Disruptor;

	public class JournalHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
		}
	}
}