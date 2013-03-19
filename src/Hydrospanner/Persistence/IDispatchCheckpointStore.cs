namespace Hydrospanner.Persistence
{
	public interface IDispatchCheckpointStore
	{
		void Save(long sequence);
	}
}