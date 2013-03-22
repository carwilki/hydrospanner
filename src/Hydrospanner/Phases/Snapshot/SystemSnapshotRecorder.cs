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
				this.currentSnapshot = new BinaryWriter(new BufferedStream(this.file.Create(this.pathToCurrentSnapshot)));
				this.currentSnapshot.Write(expectedItems);
			});
		}

		public void Record(SnapshotItem item)
		{
			Attempt(() =>
			{
				if (this.currentSnapshot == null)
					return;

				var typeName = item.Memento.GetType().AssemblyQualifiedName ?? string.Empty;
				this.currentSnapshot.Write(typeName.Length);
				this.currentSnapshot.Write(typeName.ToByteArray());

				this.currentSnapshot.Write(item.Serialized.Length);
				this.currentSnapshot.Write(item.Serialized);
			});
		}

		public void FinishRecording(int generation = 0, long sequence = 0)
		{
			Attempt(() =>
			{
				if (this.currentSnapshot == null)
					return;

				this.CloseSnapshot();
				this.FingerprintSnapshot(generation, sequence);
			});
		}

		private void FingerprintSnapshot(int generation = 0, long sequence = 0)
		{
			var hash = this.GenerateFingerprint();
			var destination = Path.Combine(this.location, SnapshotFilenameTemplate.FormatWith(generation, sequence, hash));
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
			catch (Exception)
			{
//				 TODO: log exception
			}
		}

		public SystemSnapshotRecorder(FileBase file, string location)
		{
			this.file = file;
			this.location = location;
			this.pathToCurrentSnapshot = Path.Combine(this.location, TemporaryFilename);
		}

		const string SnapshotFilenameTemplate = "{0}-{1}-{2}";
		const string TemporaryFilename = "current_snapshot";
		private readonly FileBase file;
		private readonly string location;
		private readonly string pathToCurrentSnapshot;
		private BinaryWriter currentSnapshot;
	}
}