namespace Hydrospanner.Wireup
{
	using System;
	using System.Reflection;
	using Persistence;
	using log4net;

	public class Wireup
	{
		public Wireup(ConventionWireupParameters conventionWireup)
		{
			Log.Info("Preparing to bootstrap the system.");

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

		public IDisposable Start()
		{
			try
			{
				this.bootstrapper.Start(this.info);
				return this.bootstrapper;
			}
			catch (Exception)
			{
				this.bootstrapper.Dispose();
				throw;
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(Bootstrapper));
		private readonly Bootstrapper bootstrapper;
		private readonly BootstrapInfo info;
	}
}