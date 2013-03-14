namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;
	using System.IO.Abstractions;

	public class SystemSnapshotRecorder : ISnapshotRecorder
	{
		public void Record(SnapshotItem item)
		{
			var snapshotPath = Path.Combine(this.location, this.latestIteration + "-" + item.CurrentSequence);

			using (this.currentSnapshot = this.file.Create(snapshotPath))
			using (var writer = new BinaryWriter(this.currentSnapshot))
			{
				writer.Write(item.MementosRemaining + 1);
				
				var typeName = item.Memento.GetType().AssemblyQualifiedName ?? string.Empty;
				writer.Write(typeName.Length);
				writer.Write(typeName.ToByteArray());

				writer.Write(item.Serialized.Length);
				writer.Write(item.Serialized);
			}
		}

		public SystemSnapshotRecorder(FileBase file, string location, int latestIteration) // NOTE: We could derive the latestIteration by enumerating the files in location
		{
			this.file = file;
			this.location = location;
			this.latestIteration = latestIteration;
		}

		readonly FileBase file;
		readonly string location;
		int latestIteration;
		bool recording;
		string pathToCurrentSnapshot;
		int itemsInCurrentSnapshot;
		Stream currentSnapshot;
	}
}