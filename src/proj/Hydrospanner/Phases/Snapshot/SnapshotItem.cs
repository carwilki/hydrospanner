namespace Hydrospanner.Phases.Snapshot
{
	using Serialization;

	internal sealed class SnapshotItem
	{
		public bool IsPublicSnapshot { get; set; }
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }

		public string Key { get; set; }
		public object Memento { get; set; }
		public byte[] Serialized { get; set; }

		public void AsPublicSnapshot(string key, object memento)
		{
			this.Clear();
			this.IsPublicSnapshot = true;
			this.Key = key;
			this.Memento = memento;
		}

		public void AsPartOfSystemSnapshot(long sequence, int remaining, string key, object memento)
		{
			this.Clear();
			this.Key = key;
			this.Memento = memento;
			this.CurrentSequence = sequence;
			this.MementosRemaining = remaining;
		}

		private void Clear()
		{
			this.IsPublicSnapshot = false;
			this.Key = null;
			this.Memento = null;
			this.Serialized = null;
			this.CurrentSequence = this.MementosRemaining = 0;
		}

		public void Serialize(ISerializer serializer)
		{
			this.Serialized = serializer.Serialize(this.Memento);
		}
	}
}