﻿namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;

	public class SystemSnapshotLoader
	{
		public virtual SystemSnapshotStreamReader Load(long maxSequence, int currentGeneration)
		{
			var files = this.directory.GetFiles(this.path, this.searchPattern, SearchOption.TopDirectoryOnly);
			
			var snapshots = files
				.Select(ParsedSystemSnapshotFilename.Parse)
				.Where(x => x != null && x.Sequence <= maxSequence && x.Generation <= currentGeneration);

			var mostRecentViableSnapshot = snapshots
				.OrderByDescending(x => x.Generation)
				.Select(this.OpenOrDefault)
				.FirstOrDefault(x => x != null && x.Count > 0);

			return mostRecentViableSnapshot ?? new SystemSnapshotStreamReader();
		}

		private SystemSnapshotStreamReader OpenOrDefault(ParsedSystemSnapshotFilename snapshot)
		{
			var fileStream = new BufferedStream(this.file.OpenRead(snapshot.FullPath), BufferSize);

			return SystemSnapshotStreamReader.Open(snapshot.Sequence, snapshot.Generation, snapshot.Hash, fileStream);
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