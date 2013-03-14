namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	internal class SnapshotLoader
	{
		public SnapshotStreamReader Load(long maxSequence)
		{
			var files = this.directory.GetFiles(this.path, this.searchPattern, SearchOption.TopDirectoryOnly);
			
			var snapshots = files
				.Select(ParsedSnapshotFilename.Parse)
				.Where(x => x != null && x.Sequence <= maxSequence);

			var mostRecentViableSnapshot = snapshots
				.OrderByDescending(x => x.Iteration)
				.Select(this.OpenOrDefault)
				.FirstOrDefault(x => x != null && x.Count > 0);

			return mostRecentViableSnapshot ?? new SnapshotStreamReader();
		}

		private SnapshotStreamReader OpenOrDefault(ParsedSnapshotFilename snapshot)
		{
			var fileStream = new BufferedStream(this.file.OpenRead(snapshot.FullPath), BufferSize);

			return SnapshotStreamReader.Open(snapshot.Sequence, snapshot.Iteration, snapshot.Hash, fileStream);
		}

		public SnapshotLoader(DirectoryBase directory, FileBase file, string path)
		{
			this.directory = directory;
			this.file = file;
			this.path = path;
			this.searchPattern = WildcardPattern;
		}

		const string WildcardPattern = "*";
		const int BufferSize = 1024 * 10;
		readonly DirectoryBase directory;
		readonly FileBase file;
		readonly string path;
		readonly string searchPattern;
	}
}