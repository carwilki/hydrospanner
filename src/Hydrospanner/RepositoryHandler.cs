namespace Hydrospanner
{
	using Disruptor;

	public class RepositoryHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.DuplicateMessage)
				return;
		}
	}
}