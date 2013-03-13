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
				.OrderByDescending(x => x)
				.Select(this.OpenOrBlank)
				.FirstOrDefault(x => x.Count > 0) ?? new SnapshotStreamReader();
		}

		private SnapshotStreamReader OpenOrBlank(string fullPath)
		{
			var filename = Path.GetFileNameWithoutExtension(fullPath) ?? string.Empty;
			var values = filename.Split(FieldDelimiter.ToCharArray());
			if (values.Length != SnapshotFilenameFieldCount)
				return new SnapshotStreamReader();

			long sequence;
			if (!long.TryParse(values[SequenceField], out sequence))
				return new SnapshotStreamReader();

			var hash = values[HashField];
			var fileStream = this.file.OpenRead(fullPath);

			return SnapshotStreamReader.Open(sequence, hash, fileStream) ?? new SnapshotStreamReader();
		}

		public SnapshotLoader(DirectoryBase directory, FileBase file, string path, string prefix)
		{
			this.directory = directory;
			this.file = file;
			this.path = path;
			this.searchPattern = prefix + FieldDelimiter + WildcardPattern;
		}

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