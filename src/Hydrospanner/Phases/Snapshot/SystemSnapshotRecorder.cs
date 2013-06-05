namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	internal class SystemSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
			Attempt(() =>
			{
				if (this.currentSnapshot != null)
					this.CloseSnapshot();

				this.file.Delete(this.pathToCurrentSnapshot);
				this.currentSnapshot = new BinaryWriter(new BufferedStream(
					this.file.Create(this.pathToCurrentSnapshot), SnapshotBufferSize));
				this.currentSnapshot.Write(expectedItems);
			});
		}

		public void Record(SnapshotItem item)
		{
			Attempt(() =>
			{
				if (this.currentSnapshot == null)
					return;

				var keyBytes = item.Key.ToByteArray();
				this.currentSnapshot.Write(keyBytes.Length);
				this.currentSnapshot.Write(keyBytes);

				var typeBytes = item.MementoType.ToByteArray();
				this.currentSnapshot.Write(typeBytes.Length);
				this.currentSnapshot.Write(typeBytes);

				var serializedLength = item.Serialized == null ? 0 : item.Serialized.Length;
				this.currentSnapshot.Write(serializedLength);
				if (item.Serialized != null && serializedLength > 0)
					this.currentSnapshot.Write(item.Serialized);
			});
		}

		public void FinishRecording(long sequence = 0)
		{
			Attempt(() =>
			{
				if (this.currentSnapshot == null)
					return;

				this.CloseSnapshot();
				this.FingerprintSnapshot(sequence);
				this.CleanupOldSnapshots(sequence);
			});
		}

		private void CloseSnapshot()
		{
			this.currentSnapshot.Flush();
			this.currentSnapshot.Dispose();
			this.currentSnapshot = null;
		}
		private void FingerprintSnapshot(long sequence = 0)
		{
			var hash = this.GenerateFingerprint();
			var destination = Path.Combine(this.location, SnapshotFilenameTemplate.FormatWith(sequence, hash));
			this.file.Move(this.pathToCurrentSnapshot, destination);
		}
		private string GenerateFingerprint()
		{
			using (var hasher = new SHA1Managed())
			using (var fileStream = this.file.OpenRead(this.pathToCurrentSnapshot))
				return new SoapHexBinary(hasher.ComputeHash(fileStream)).ToString();
		}
		private void CleanupOldSnapshots(long currentSequence)
		{
			var snapshots = this.directory
				.GetFiles(this.location, WildcardPattern, SearchOption.TopDirectoryOnly)
				.Select(ParsedSystemSnapshotFilename.Parse)
				.Where(x => x != null)
				.OrderByDescending(x => x.Sequence)
				.ToArray();

			this.CleanupOldSnapshots(snapshots.Where(x => x.Sequence < currentSequence).Skip(MaxSystemSnapshots - 1));
			this.CleanupOldSnapshots(snapshots.Where(x => x.Sequence > currentSequence));
		}
		private void CleanupOldSnapshots(IEnumerable<ParsedSystemSnapshotFilename> snapshots)
		{
			foreach (var item in snapshots)
				this.file.Delete(item.FullPath);
		}

		private static void Attempt(Action callback)
		{
			try
			{
				callback();
			}
			catch
			{
				// ReSharper disable RedundantJumpStatement
				return;
				// ReSharper restore RedundantJumpStatement
			}
		}

		public SystemSnapshotRecorder(DirectoryBase directory, FileBase file, string location)
		{
			this.directory = directory;
			this.file = file;
			this.location = location;
			this.pathToCurrentSnapshot = Path.Combine(this.location, TemporaryFilename);
		}

		private const string WildcardPattern = "*";
		private const string SnapshotFilenameTemplate = "{0}-{1}";
		private const string TemporaryFilename = "current_snapshot";
		private const int SnapshotBufferSize = 1024 * 1024 * 8;
		private const int MaxSystemSnapshots = 5;
		readonly DirectoryBase directory;
		private readonly FileBase file;
		private readonly string location;
		private readonly string pathToCurrentSnapshot;
		private BinaryWriter currentSnapshot;
	}
}