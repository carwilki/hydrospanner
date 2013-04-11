namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;

	internal class ParsedSystemSnapshotFilename
	{
		public string FullPath { get; set; }
		public long Sequence { get; set; }
		public string Hash { get; set; }

		public static ParsedSystemSnapshotFilename Parse(string path)
		{
			var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
			var values = filename.Split(FieldDelimiter.ToCharArray());
			if (values.Length != SnapshotFilenameFieldCount)
				return null;

			long sequence;
			if (!long.TryParse(values[SequenceField], out sequence))
				return null;

			return new ParsedSystemSnapshotFilename
			{
				FullPath = path,
				Sequence = sequence,
				Hash = values[HashField]
			};
		}

		const int SequenceField = 0;
		const int HashField = 1;
		const int SnapshotFilenameFieldCount = 2;
		const string FieldDelimiter = "-";
	}
}