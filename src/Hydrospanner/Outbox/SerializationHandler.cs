namespace Hydrospanner.Outbox
{
	using Disruptor;

	public class SerializationHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
		}
	}
}