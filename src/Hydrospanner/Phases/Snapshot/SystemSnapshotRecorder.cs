namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.IO;
	using System.IO.Abstractions;
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
			});
		}

		private void FingerprintSnapshot(long sequence = 0)
		{
			var hash = this.GenerateFingerprint();
			var destination = Path.Combine(this.location, SnapshotFilenameTemplate.FormatWith(sequence, hash));
			this.file.Move(this.pathToCurrentSnapshot, destination);
		}

		private void CloseSnapshot()
		{
			this.currentSnapshot.Flush();
			this.currentSnapshot.Dispose();
			this.currentSnapshot = null;
		}

		private string GenerateFingerprint()
		{
			using (var hasher = new SHA1Managed())
			using (var fileStream = this.file.OpenRead(this.pathToCurrentSnapshot))
				return new SoapHexBinary(hasher.ComputeHash(fileStream)).ToString();
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

		public SystemSnapshotRecorder(FileBase file, string location)
		{
			this.file = file;
			this.location = location;
			this.pathToCurrentSnapshot = Path.Combine(this.location, TemporaryFilename);
		}

		const string SnapshotFilenameTemplate = "{0}-{1}";
		const string TemporaryFilename = "current_snapshot";
		const int SnapshotBufferSize = 1024 * 1024 * 8;
		private readonly FileBase file;
		private readonly string location;
		private readonly string pathToCurrentSnapshot;
		private BinaryWriter currentSnapshot;
	}
}