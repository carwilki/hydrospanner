namespace Hydrospanner
{
	public class SnapshotMessage
	{
		public bool IsolatedSnapshot { get; set; }
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }
		public object Memento { get; set; }

		public void Clear()
		{
			this.IsolatedSnapshot = false;
			this.CurrentSequence = 0;
			this.Memento = null;
			this.MementosRemaining = 0;
		}
	}
}