namespace Hydrospanner.Phases.Snapshot
{
	public sealed class NullSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
		}
		public void Record(SnapshotItem item)
		{
		}
		public void FinishRecording(long sequence = 0)
		{
		}
	}
}