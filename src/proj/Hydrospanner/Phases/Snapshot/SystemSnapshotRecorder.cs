namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	public class SystemSnapshotRecorder : ISnapshotRecorder
	{
		public void Record(SnapshotItem item)
		{
			if (!this.recording)
				this.InitializeSnapshot(item);

			this.RecordItem(item);
			
			if (item.MementosRemaining == 0)
				this.FinalizeSnapshot();
		}

		private void InitializeSnapshot(SnapshotItem item)
		{
			this.pathToCurrentSnapshot = Path.Combine(this.location, this.latestIteration + "-" + item.CurrentSequence);
			this.currentSnapshot = new BinaryWriter(new BufferedStream(this.file.Create(this.pathToCurrentSnapshot)));
			this.currentSnapshot.Write(item.MementosRemaining + 1);
			this.recording = true;
		}

		private void RecordItem(SnapshotItem item)
		{
			var typeName = item.Memento.GetType().AssemblyQualifiedName ?? string.Empty;
			this.currentSnapshot.Write(typeName.Length);
			this.currentSnapshot.Write(typeName.ToByteArray());

			this.currentSnapshot.Write(item.Serialized.Length);
			this.currentSnapshot.Write(item.Serialized);
		}

		private void FinalizeSnapshot()
		{
			this.currentSnapshot.Dispose();
			var hash = this.GenerateFingerprint();
			this.file.Move(this.pathToCurrentSnapshot, this.pathToCurrentSnapshot + "-" + hash);
			this.latestIteration++;
			this.recording = false;
		}

		private string GenerateFingerprint()
		{
			using (var hasher = new SHA1Managed())
			using (var fileStream = this.file.OpenRead(this.pathToCurrentSnapshot))
				return new SoapHexBinary(hasher.ComputeHash(fileStream)).ToString();
		}

		public SystemSnapshotRecorder(FileBase file, string location, int latestIteration) // NOTE: We could derive the latestIteration by enumerating the files in location
		{
			this.file = file;
			this.location = location;
			this.latestIteration = latestIteration;
		}

		private readonly FileBase file;
		private readonly string location;
		private int latestIteration;
		private bool recording;
		private string pathToCurrentSnapshot;
		private BinaryWriter currentSnapshot;
	}
}