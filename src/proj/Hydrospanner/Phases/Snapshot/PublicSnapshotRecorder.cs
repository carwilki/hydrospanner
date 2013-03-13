namespace Hydrospanner.Phases.Snapshot
{
	public class PublicSnapshotRecorder : ISnapshotRecorder
	{
		public void Record(SnapshotItem item)
		{
		}

		public PublicSnapshotRecorder(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		readonly ISnapshotRecorder recorder;
	}
}