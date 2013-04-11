namespace Hydrospanner.Phases.Snapshot
{
	public interface ISnapshotRecorder
	{
		void StartRecording(int expectedItems);
		void Record(SnapshotItem item);
		void FinishRecording(long sequence = 0);
	}
}