namespace Hydrospanner.Snapshot
{
	public class SnapshotItem
	{
		public bool IsPublicSnapshot { get; private set; }
		public long CurrentSequence { get; private set; }
		public int MementosRemaining { get; private set; }

		public string Key { get; private set; }
		public object Memento { get; private set; }
		public byte[] Serialized { get; private set; }

		public void AsPublicSnapshot(string key, object memento)
		{
			this.IsPublicSnapshot = true;
			this.Key = key;
			this.Memento = memento;
			
			this.CurrentSequence = 0;
			this.MementosRemaining = 0;
			this.Serialized = null;
		}

		public void Serialize(JsonSerializer serializer)
		{
			this.Serialized = serializer.Serialize(this.Memento);
		}
	}
}