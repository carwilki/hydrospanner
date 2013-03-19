namespace Hydrospanner.Persistence
{
	public interface IDispatchCheckpointStore
	{
		long Load();
		void Save(long sequence);
	}
}