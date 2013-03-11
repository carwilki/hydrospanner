namespace Hydrospanner
{
	using Disruptor;

	public class PublicSnapshotHandler : IEventHandler<SnapshotMessage>
	{
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			if (!data.PublicSnapshot)
				return;

			// push to KVS @ endOfBatch
		}
	}
}