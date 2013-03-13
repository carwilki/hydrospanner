namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	public class SnapshotLoader
	{
		public SnapshotStreamReader LoadMostRecent()
		{
			return this.directory
				.GetFiles(this.path, this.searchPattern, SearchOption.TopDirectoryOnly)
				.Select(this.OpenOrBlank)
				.Where(x => x != null)
				.OrderByDescending(x => x.Iteration)
				.FirstOrDefault(x => x.Count > 0) ?? new SnapshotStreamReader();
		}

		private SnapshotStreamReader OpenOrBlank(string fullPath)
		{
			var filename = Path.GetFileNameWithoutExtension(fullPath) ?? string.Empty;
			var values = filename.Split(FieldDelimiter.ToCharArray());
			if (values.Length != SnapshotFilenameFieldCount)
				return null;

			int iteration;
			if (!int.TryParse(values[IterationField], out iteration))
				return null;

			long sequence;
			if (!long.TryParse(values[SequenceField], out sequence))
				return null;

			var hash = values[HashField];
			var fileStream = this.file.OpenRead(fullPath);

			return SnapshotStreamReader.Open(sequence, iteration, hash, fileStream);
		}

		public SnapshotLoader(DirectoryBase directory, FileBase file, string path)
		{
			this.directory = directory;
			this.file = file;
			this.path = path;
			this.searchPattern = WildcardPattern;
		}

		const int IterationField = 0;
		const int SequenceField = 1;
		const int HashField = 2;
		const int SnapshotFilenameFieldCount = 3;
		const string FieldDelimiter = "-";
		const string WildcardPattern = "*";
		readonly DirectoryBase directory;
		readonly FileBase file;
		readonly string path;
		readonly string searchPattern;
	}
}