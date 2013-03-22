namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Persistence;

	public class SnapshotBootstrapper
	{
		public virtual BootstrapInfo RestoreSnapshots(BootstrapInfo info, IRepository repository)
		{
			return null;
		}

		public SnapshotBootstrapper(SnapshotFactory snapshotFactory, DisruptorFactory disruptorFactory)
		{
			if (snapshotFactory == null)
				throw new ArgumentNullException("snapshotFactory");

			if (disruptorFactory == null)
				throw new ArgumentNullException("disruptorFactory");

			this.snapshotFactory = snapshotFactory;
			this.disruptorFactory = disruptorFactory;
		}
		protected SnapshotBootstrapper()
		{
		}

		readonly SnapshotFactory snapshotFactory;
		readonly DisruptorFactory disruptorFactory;
	}
}