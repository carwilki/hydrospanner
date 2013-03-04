namespace Hydrospanner.Phase3
{
	using Disruptor;

	public class DispatchHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			// only commit at end of batch
		}
	}
}