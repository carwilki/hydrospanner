namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;

	public class PublicSnapshotDispatchHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			// FUTURE: this will transactionally publish snapshots to the wire in a standard format
		}
	}
}