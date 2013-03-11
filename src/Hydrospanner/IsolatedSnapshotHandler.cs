namespace Hydrospanner
{
	using Disruptor;

	public class IsolatedSnapshotHandler : IEventHandler<SnapshotMessage>
	{
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			if (!data.IsolatedSnapshot)
				return;
		}
	}
}