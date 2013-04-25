namespace Hydrospanner.Timeout
{
	public interface ITimeoutWatcher
	{
		void Abort(IHydratable hydratable);
	}
}