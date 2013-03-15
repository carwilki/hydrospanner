namespace Hydrospanner.Phases.Snapshot
{
	internal class PublicSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
		}

		public void Record(SnapshotItem item)
		{
		}

		public void FinishRecording(int iteration = 0, long sequence = 0)
		{
		}
	}
}