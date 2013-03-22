namespace Hydrospanner.Phases.Bootstrap
{
	using Hydrospanner.Persistence;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, object snapshotRing, object journalRing, IRepository repository)
		{
			throw new System.NotImplementedException();
		}
	}
}