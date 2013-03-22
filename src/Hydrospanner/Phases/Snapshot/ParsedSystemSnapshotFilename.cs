namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;

	internal class ParsedSystemSnapshotFilename
	{
		public string FullPath { get; set; }
		public int Generation { get; set; }
		public long Sequence { get; set; }
		public string Hash { get; set; }

		public static ParsedSystemSnapshotFilename Parse(string path)
		{
			var filename = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
			var values = filename.Split(FieldDelimiter.ToCharArray());
			if (values.Length != SnapshotFilenameFieldCount)
				return null;

			int generation;
			if (!int.TryParse(values[GenerationField], out generation))
				return null;

			long sequence;
			if (!long.TryParse(values[SequenceField], out sequence))
				return null;

			return new ParsedSystemSnapshotFilename
			{
				FullPath = path,
				Generation = generation,
				Sequence = sequence,
				Hash = values[HashField]
			};
		}

		const int GenerationField = 0;
		const int SequenceField = 1;
		const int HashField = 2;
		const int SnapshotFilenameFieldCount = 3;
		const string FieldDelimiter = "-";
	}
}