namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;

	internal class ParsedSystemSnapshotFilename
	{
		public string FullPath { get; set; }
		public int Iteration { get; set; }
		public long Sequence { get; set; }
		public string Hash { get; set; }

		public static ParsedSystemSnapshotFilename Parse(string path)
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

			return new ParsedSystemSnapshotFilename
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