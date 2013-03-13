namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;

	public class SystemSnapshotRecorder : ISnapshotRecorder
	{
		// will manage opening, writing, and closing, fingerprinting of all outgoing snapshots.

		public void Record(SnapshotItem item)
		{
			// if not recording:
			//	open a new stream, (FILE)
			//  derive how many items based on the incoming item (remaining)
			//	write first item (stream)

			// else:
			//	write item (stream)
			//	if item.Remaining == 0:
			//		finalize current snapshot (dispose (stream), fingerprint w/ hash, ++latestItereation, and item.message-sequence (FILE))
		}

		public SystemSnapshotRecorder(FileBase file, string location, int latestIteration) // NOTE: We could derive the latestIteration by enumerating the files in location
		{
			this.file = file;
			this.location = location;
			this.latestIteration = latestIteration;
		}

		readonly FileBase file;
		readonly string location;
		int latestIteration;
		bool recording;
		string pathToCurrentSnapshot;
		int itemsInCurrentSnapshot;
		Stream currentSnapshot;
	}
}