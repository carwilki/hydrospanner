namespace Hydrospanner.Phases.Snapshot
{
	internal class PublicSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(long sequence, int iteration, int expectedItems)
		{
		}

		public void Record(SnapshotItem item)
		{
		}

		public void FinishRecording()
		{
		}
	}
}