namespace Hydrospanner
{
	public class SnapshotMessage
	{
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }
		public object Memento { get; set; }
	}
}