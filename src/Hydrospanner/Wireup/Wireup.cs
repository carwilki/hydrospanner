namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using log4net;
	using Persistence;
	using Phases.Transformation;

	public class Wireup : IDisposable
	{
		public static Wireup Initialize(params Assembly[] assembliesToSan)
		{
			return Initialize(new ConventionWireupParameters(), assembliesToSan);
		}
		public static Wireup Initialize(ConventionWireupParameters configuration, params Assembly[] assembliesToScan)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			var scan = new List<Assembly>(assembliesToScan);
			if (scan.Count == 0)
				scan.Add(Assembly.GetCallingAssembly());

			return new Wireup(configuration, scan);
		}

		public void Execute()
		{
			if (this.bootstrapper.Start(this.info))
				return;

			Log.Fatal("Unable to start the hydrospanner, one or more serialization errors occurred during the bootstrap process.");
		}

		private Wireup(ConventionWireupParameters conventionWireup, IEnumerable<Assembly> assemblies)
		{
			Log.Info("Preparing to bootstrap the system.");
			var repository = new DefaultRepository(new ConventionRoutingTable(assemblies));
			var persistenceFactory = new PersistenceFactory(conventionWireup.JournalConnectionName, conventionWireup.DuplicateWindow, conventionWireup.JournalBatchSize);
			var snapshotFactory = new SnapshotFactory(conventionWireup.SnapshotLocation, conventionWireup.PublicSnapshotConnectionName);
			var persistenceBootstrapper = new PersistenceBootstrapper(persistenceFactory);

			Log.Info("Connecting to message store.");
			this.info = persistenceBootstrapper.Restore();
			var duplicates = new DuplicateStore(conventionWireup.DuplicateWindow, this.info.DuplicateIdentifiers);
			var messagingFactory = new MessagingFactory(conventionWireup.NodeId, conventionWireup.BrokerAddress, conventionWireup.SourceQueueName, duplicates);

			Log.Info("Loading bootstrap parameters.");
			var messageStore = persistenceFactory.CreateMessageStore(this.info.SerializedTypes);
			var disruptorFactory = new DisruptorFactory(messagingFactory, persistenceFactory, snapshotFactory, conventionWireup.DuplicateWindow, conventionWireup.SystemSnapshotFrequency);
			var snapshotBootstrapper = new SnapshotBootstrapper(snapshotFactory, disruptorFactory);
			var messageBootstrapper = new MessageBootstrapper(messageStore, disruptorFactory);

			this.bootstrapper = new Bootstrapper(repository, disruptorFactory, snapshotBootstrapper, messageBootstrapper, messagingFactory);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.bootstrapper.Dispose();
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(Bootstrapper));
		private readonly Bootstrapper bootstrapper;
		private readonly BootstrapInfo info;
		private bool disposed;
	}
}