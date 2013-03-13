namespace Hydrospanner.Phases.Snapshot
{
	public interface ISnapshotRecorder
	{
		void Record(SnapshotItem item);
	}
}