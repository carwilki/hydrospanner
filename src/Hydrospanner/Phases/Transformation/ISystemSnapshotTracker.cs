namespace Hydrospanner.Phases.Transformation
{
	public interface ISystemSnapshotTracker
	{
		void Track(long sequence);
	}
}