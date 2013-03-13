namespace Hydrospanner.Phases.Snapshot
{
	using Hydrospanner.Serialization;

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
			this.Clear();
			this.IsPublicSnapshot = true;
			this.Key = key;
			this.Memento = memento;
		}

		private void Clear()
		{
			this.IsPublicSnapshot = false;
			this.Key = null;
			this.Memento = null;
			this.Serialized = null;
			this.CurrentSequence = this.MementosRemaining = 0;
		}

		public void Serialize(JsonSerializer serializer)
		{
			this.Serialized = serializer.Serialize(this.Memento);
		}
	}
}