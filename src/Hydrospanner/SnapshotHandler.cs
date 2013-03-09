namespace Hydrospanner
{
	using Disruptor;

	public class SnapshotHandler : IEventHandler<SnapshotMessage>
	{
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			// these will only ever come in a batch; just stream until the last item is reached and then close the stream
			// at the first item, open a file and then start streaming to the file.
		}
	}
}