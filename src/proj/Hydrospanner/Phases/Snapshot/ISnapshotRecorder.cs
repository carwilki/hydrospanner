namespace Hydrospanner.Phases.Snapshot
{
	internal interface ISnapshotRecorder
	{
		void StartRecording(int expectedItems);
		void Record(SnapshotItem item);
		void FinishRecording(int iteration = 0, long sequence = 0);
	}
}