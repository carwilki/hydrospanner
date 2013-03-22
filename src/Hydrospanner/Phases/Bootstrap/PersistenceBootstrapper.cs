namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Hydrospanner.Configuration;
	using Hydrospanner.Persistence;

	public class PersistenceBootstrapper
	{
		public virtual BootstrapInfo Restore()
		{
			throw new NotImplementedException();
		}

		public PersistenceBootstrapper(PersistenceFactory factory)
		{
			this.factory = factory;
		}

		protected PersistenceBootstrapper()
		{
		}

		readonly PersistenceFactory factory;
	}
}