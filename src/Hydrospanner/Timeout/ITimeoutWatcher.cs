namespace Hydrospanner.Timeout
{
	public interface ITimeoutWatcher
	{
		IHydratable Abort(IHydratable hydratable);
		object Filter(string key, object message);
	}
}