namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	public class SystemSnapshotLoader
	{
		public virtual SystemSnapshotStreamReader Load(long maxSequence)
		{
			var snapshots = this.directory
				.GetFiles(this.path, this.searchPattern, SearchOption.TopDirectoryOnly)
				.Select(ParsedSystemSnapshotFilename.Parse)
				.Where(x => x != null && x.Sequence <= maxSequence);

			var openSnapshot = snapshots
				.OrderByDescending(x => x.Sequence)
				.Select(this.OpenOrDefault)
				.FirstOrDefault(x => x != null && x.Count > 0);

			return openSnapshot ?? new SystemSnapshotStreamReader();
		}

		private SystemSnapshotStreamReader OpenOrDefault(ParsedSystemSnapshotFilename snapshot)
		{
			Stream stream = null;
			SystemSnapshotStreamReader reader = null;

			try
			{
				// FUTURE: buffer according to file size *up to* 64 MB.
				stream = new BufferedStream(this.file.OpenRead(snapshot.FullPath), MaxBufferSize);
				return reader = SystemSnapshotStreamReader.Open(snapshot.Sequence, snapshot.Hash, stream);
			}
			finally
			{
				if (stream != null && reader == null)
					stream.Dispose();
			}
		}

		public SystemSnapshotLoader(DirectoryBase directory, FileBase file, string path)
		{
			this.directory = directory;
			this.file = file;
			this.path = path;
			this.searchPattern = WildcardPattern;
		}

		private const string WildcardPattern = "*";
		private const int MaxBufferSize = 1024 * 1024 * 64;
		private readonly DirectoryBase directory;
		private readonly FileBase file;
		private readonly string path;
		private readonly string searchPattern;
	}
}