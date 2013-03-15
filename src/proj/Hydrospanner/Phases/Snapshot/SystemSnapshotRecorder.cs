namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	internal class SystemSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
			if (this.currentSnapshot != null)
				this.CloseSnapshot();

			this.pathToCurrentSnapshot = Path.Combine(this.location, "current_snapshot");
			this.currentSnapshot = new BinaryWriter(new BufferedStream(this.file.Create(this.pathToCurrentSnapshot)));
			this.currentSnapshot.Write(expectedItems);
		}

		public void Record(SnapshotItem item)
		{
			if (this.currentSnapshot == null)
				return;

			var typeName = item.Memento.GetType().AssemblyQualifiedName ?? string.Empty;
			this.currentSnapshot.Write(typeName.Length);
			this.currentSnapshot.Write(typeName.ToByteArray());

			this.currentSnapshot.Write(item.Serialized.Length);
			this.currentSnapshot.Write(item.Serialized);
		}

		public void FinishRecording(int iteration = 0, long sequence = 0)
		{
			if (this.currentSnapshot == null)
				return;

			this.CloseSnapshot();
			this.FingerprintSnapshot(iteration, sequence);
		}

		void FingerprintSnapshot(int iteration = 0, long sequence = 0)
		{
			var hash = this.GenerateFingerprint();
			var destination = Path.Combine(location, "{0}-{1}-{2}".FormatWith(iteration, sequence, hash));
			this.file.Move(this.pathToCurrentSnapshot, destination);
		}

		void CloseSnapshot()
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

		public SystemSnapshotRecorder(FileBase file, string location)
		{
			this.file = file;
			this.location = location;
		}

		private readonly FileBase file;
		private readonly string location;
		private string pathToCurrentSnapshot;
		private BinaryWriter currentSnapshot;
	}
}