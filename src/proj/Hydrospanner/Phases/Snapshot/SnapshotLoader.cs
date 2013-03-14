namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	public class SnapshotLoader
	{
		public SnapshotStreamReader Load(long maxSequence, int minIteration)
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

	public class ParsedSnapshotFilename
	{
		public string FullPath { get; set; }
		public int Iteration { get; set; }
		public long Sequence { get; set; }
		public string Hash { get; set; }

		public static ParsedSnapshotFilename Parse(string path)
		{
			var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
			var values = filename.Split(FieldDelimiter.ToCharArray());
			if (values.Length != SnapshotFilenameFieldCount)
				return null;

			int iteration;
			if (!int.TryParse(values[IterationField], out iteration))
				return null;

			long sequence;
			if (!long.TryParse(values[SequenceField], out sequence))
				return null;

			return new ParsedSnapshotFilename
			{
				FullPath = path,
				Iteration = iteration,
				Sequence = sequence,
				Hash = values[HashField]
			};
		}

		const int IterationField = 0;
		const int SequenceField = 1;
		const int HashField = 2;
		const int SnapshotFilenameFieldCount = 3;
		const string FieldDelimiter = "-";
	}
}