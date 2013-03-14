namespace Hydrospanner.Phases.Snapshot
{
	internal interface ISnapshotRecorder
	{
		void Record(SnapshotItem item);
	}
}