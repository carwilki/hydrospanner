namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Hydrospanner.Configuration;
	using Hydrospanner.Persistence;

	public class PersistenceBootstrapper
	{
		public virtual BootstrapInfo Restore()
		{
			return this.factory.CreateBootstrapStore().Load();
		}

		public PersistenceBootstrapper(PersistenceFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			this.factory = factory;
		}

		protected PersistenceBootstrapper()
		{
		}

		readonly PersistenceFactory factory;
	}
}