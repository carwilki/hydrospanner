namespace Hydrospanner.Phases.Snapshot
{
	public interface ISnapshotRecorder
	{
		void StartRecording(int expectedItems);
		void Record(SnapshotItem item);
		void FinishRecording(int generation = 0, long sequence = 0);
	}
}