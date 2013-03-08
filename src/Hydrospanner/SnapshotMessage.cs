namespace Hydrospanner
{
	public class SnapshotMessage
	{
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }
		public object Memento { get; set; }

		public void Clear()
		{
			this.CurrentSequence = 0;
			this.Memento = null;
			this.MementosRemaining = 0;
		}
	}
}