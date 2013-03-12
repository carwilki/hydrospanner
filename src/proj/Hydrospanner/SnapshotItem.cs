namespace Hydrospanner
{
	internal class SnapshotItem
	{
		public bool IsPublicSnapshot { get; private set; }
		public long CurrentSequence { get; private set; }
		public int MementosRemaining { get; private set; }

		public string Key { get; private set; }
		public object Memento { get; private set; }
		public byte[] Serialized { get; private set; }
	}
}