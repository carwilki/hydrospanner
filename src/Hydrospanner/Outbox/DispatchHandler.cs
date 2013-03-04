namespace Hydrospanner.Outbox
{
	using Disruptor;

	public class DispatchHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			// buffer locally, at end of batch, push buffer to rabbitmq and commit trx
			// if broker disconnects, re-connect and retry locally buffered messages and commit (infinite loop)
		}
	}
}