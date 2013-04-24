namespace Hydrospanner.Timeout
{
	public sealed class NullTimeoutWatcher : ITimeoutWatcher
	{
		public void Add(ITimeoutHydratable hydratable)
		{
		}
		public void Remove(string key)
		{
		}
	}
}