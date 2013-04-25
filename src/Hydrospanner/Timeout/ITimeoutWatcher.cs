namespace Hydrospanner.Timeout
{
	public interface ITimeoutWatcher
	{
		void Abort(string key);
	}
}