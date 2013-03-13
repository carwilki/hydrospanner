namespace Hydrospanner.Phases.Snapshot
{
	using System.IO.Abstractions;

	public class SystemSnapshotRecorder : ISnapshotRecorder
	{
		public void Record(SnapshotItem item)
		{
			// will manage opening, writing, and closing of all outgoing snapshots.
			// has to track items remaining in the current snapshot
			// has to track the current snapshot number/version (iteration
			// has to track the current message sequence?
		}

		public SystemSnapshotRecorder(FileBase file, string location, string lastPrefix)
		{
		}
	}
}