namespace Hydrospanner.Persistence
{
	public interface IDispatchCheckpointStorage
	{
		long Load();
		void Save(long sequence);
	}
}