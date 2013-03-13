namespace Hydrospanner.Phases.Snapshot
{
	using System.IO.Abstractions;

	public class SnapshotLoader
	{
		public SnapshotLoader(DirectoryBase directory, string path, string prefix)
		{
		}

		public SnapshotStreamReader LoadMostRecent()
		{
			// TODO: select most recently created viable snapshot and load it as a SnapshotStreamReader
			return new SnapshotStreamReader();
		}
	}
}