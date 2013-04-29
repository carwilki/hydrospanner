namespace Hydrospanner.Timeout
{
	public sealed class NullTimeoutWatcher : ITimeoutWatcher
	{
		public IHydratable Abort(IHydratable hydratable)
		{
			return null;
		}
		public object Filter(string key, object message)
		{
			return this;
		}
	}
}