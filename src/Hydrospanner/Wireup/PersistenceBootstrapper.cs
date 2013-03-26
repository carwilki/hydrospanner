namespace Hydrospanner.Wireup
{
	using System;
	using Persistence;

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