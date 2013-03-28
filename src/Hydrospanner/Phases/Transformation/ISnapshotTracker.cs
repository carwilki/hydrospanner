namespace Hydrospanner.Phases.Transformation
{
	public interface ISnapshotTracker
	{
		void Increment(int messages);
	}
}