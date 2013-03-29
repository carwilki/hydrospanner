namespace Hydrospanner.Phases.Transformation
{
	public interface ISnapshotTracker
	{
		void Track(long sequence);
	}
}