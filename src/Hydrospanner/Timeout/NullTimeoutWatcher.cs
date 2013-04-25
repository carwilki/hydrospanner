namespace Hydrospanner.Timeout
{
	public sealed class NullTimeoutWatcher : ITimeoutWatcher
	{
		public void Abort(string key)
		{
		}
	}
}