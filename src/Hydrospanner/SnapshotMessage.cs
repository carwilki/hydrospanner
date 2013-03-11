namespace Hydrospanner
{
	public class SnapshotMessage
	{
		public bool PublicSnapshot { get; set; }
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }

		public string Key { get; set; }
		public object Memento { get; set; }
		public byte[] Serialized { get; set; }

		public void Clear()
		{
			this.PublicSnapshot = false;
			this.CurrentSequence = 0;
			this.MementosRemaining = 0;
			this.Key = null;
			this.Memento = null;
			this.Serialized = null;
		}
	}
}