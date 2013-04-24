namespace Hydrospanner.Timeout
{
	public interface ITimeoutWatcher
	{
		void Add(ITimeoutHydratable hydratable);
		void Remove(string key);
	}
}