namespace Hydrospanner.Phases.Snapshot
{
	internal interface ISnapshotRecorder
	{
		void StartRecording(long sequence, int iteration, int expectedItems);
		void Record(SnapshotItem item);
		void FinishRecording();
	}
}