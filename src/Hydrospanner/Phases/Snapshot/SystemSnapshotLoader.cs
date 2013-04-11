namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	public class SystemSnapshotLoader
	{
		public virtual SystemSnapshotStreamReader Load(long maxSequence)
		{
			var files = this.directory.GetFiles(this.path, this.searchPattern, SearchOption.TopDirectoryOnly).ToList();
			var snapshots = files.Select(ParsedSystemSnapshotFilename.Parse).ToList();
			var viableSnapshots = snapshots.Where(x => x != null && x.Sequence <= maxSequence).ToList();
			var openSnapshots = viableSnapshots.OrderByDescending(x => x.Sequence).Select(this.OpenOrDefault).ToList();
			var first = openSnapshots.FirstOrDefault(x => x != null && x.Count > 0);
			return first ?? new SystemSnapshotStreamReader();
		}

		private SystemSnapshotStreamReader OpenOrDefault(ParsedSystemSnapshotFilename snapshot)
		{
			var fileStream = new BufferedStream(this.file.OpenRead(snapshot.FullPath), BufferSize);

			return SystemSnapshotStreamReader.Open(snapshot.Sequence, snapshot.Hash, fileStream);
		}

		public SystemSnapshotLoader(DirectoryBase directory, FileBase file, string path)
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