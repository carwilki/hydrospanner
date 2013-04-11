namespace Hydrospanner.Wireup
{
	using System;
	using System.Reflection;
	using log4net;
	using Persistence;

	public class Wireup : IDisposable
	{
		public static Wireup Initialize()
		{
			return Initialize(new ConventionWireupParameters());
		}
		public static Wireup Initialize(ConventionWireupParameters configuration)
		{
			return new Wireup(configuration);
		}

		public void Start()
		{
			this.bootstrapper.Start(this.info);
		}

		private Wireup(ConventionWireupParameters conventionWireup)
		{
			Log.Info("Preparing to bootstrap the system.");

			// TODO: it may not be the entry assembly that needs to be scanned; that may have to come as a part of the wireup
			var repository = new DefaultRepository(new ConventionRoutingTable(Assembly.GetEntryAssembly()));
			var messagingFactory = new MessagingFactory(conventionWireup.NodeId, conventionWireup.BrokerAddress, conventionWireup.SourceQueueName);
			var persistenceFactory = new PersistenceFactory(conventionWireup.JournalConnectionName, conventionWireup.DuplicateWindow, conventionWireup.JournalBatchSize);
			var persistenceBootstrapper = new PersistenceBootstrapper(persistenceFactory);

			Log.Info("Loading bootstrap parameters.");

			this.info = persistenceBootstrapper.Restore();
			var messageStore = persistenceFactory.CreateMessageStore(this.info.SerializedTypes);
			var snapshotFactory = new SnapshotFactory(conventionWireup.SnapshotGeneration, conventionWireup.SnapshotLocation, conventionWireup.PublicSnapshotConnectionName);
			var disruptorFactory = new DisruptorFactory(messagingFactory, persistenceFactory, snapshotFactory);
			var snapshotBootstrapper = new SnapshotBootstrapper(snapshotFactory, disruptorFactory);
			var messageBootstrapper = new MessageBootstrapper(messageStore, disruptorFactory, conventionWireup.SystemSnapshotFrequency);

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