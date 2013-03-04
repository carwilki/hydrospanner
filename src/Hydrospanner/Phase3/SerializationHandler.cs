namespace Hydrospanner.Phase3
{
	using Disruptor;

	public class SerializationHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			// TODO: serialize the message to a byte array
		}
	}
}