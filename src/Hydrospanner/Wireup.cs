namespace Hydrospanner
{
	using System;
	using System.Configuration;
	using System.Data.Common;
	using System.IO;
	using System.IO.Abstractions;
	using Hydrospanner.Messaging;
	using Hydrospanner.Messaging.Rabbit;
	using Hydrospanner.Persistence;
	using Hydrospanner.Persistence.SqlPersistence;
	using Hydrospanner.Phases.Snapshot;

	public class Wireup : IDisposable
	{
		public virtual IRepository Repository { get; private set; }
		public virtual SystemSnapshotLoader SystemSnapshotLoader { get; private set; }
		public virtual ISnapshotRecorder SystemSnapshotRecorder { get; private set; }
		public virtual ISnapshotRecorder PublicSnapshotRecorder { get; private set; }
		public virtual BootstrapInfo BootstrapInfo { get; private set; }
		public virtual IDispatchCheckpointStore DispatchCheckpointStore { get; private set; }
		public virtual IMessageSender MessageSender { get; private set; }
		public virtual IMessageReceiver MessageReceiver { get; private set; }
		public virtual IMessageStore MessageStore { get; private set; }

		public static Wireup Initialize(
			IRepository repository,
			short nodeId,
			Uri messageBroker,
			string sourceQueue,
			int duplicateWindow,
			string connectionName,
			string snapshotPath,
			int generation)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			if (messageBroker == null)
				throw new ArgumentNullException("messageBroker");

			if (string.IsNullOrWhiteSpace(sourceQueue))
				throw new ArgumentNullException("sourceQueue");

			if (duplicateWindow <= 0)
				throw new ArgumentOutOfRangeException("duplicateWindow");

			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			if (string.IsNullOrWhiteSpace(snapshotPath))
				throw new ArgumentNullException("snapshotPath");

			if (generation < 0)
				throw new ArgumentOutOfRangeException("generation");

			var connectionSettings = ConfigurationManager.ConnectionStrings[connectionName];
			if (connectionSettings == null)
				throw new ConfigurationErrorsException("Connection named '{0}' does not exist.".FormatWith(connectionName));

			if (string.IsNullOrWhiteSpace(connectionSettings.ProviderName) || string.IsNullOrWhiteSpace(connectionSettings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(connectionName));

			if (!Directory.Exists(snapshotPath))
				throw new DirectoryNotFoundException("Configured snapshot path directory missing.");

			var factory = DbProviderFactories.GetFactory(connectionSettings.ProviderName);
			var connector = new RabbitConnector(messageBroker);
			var bootstrapInfo = new SqlBootstrapStore(factory, connectionSettings.ConnectionString, duplicateWindow);
			var info = bootstrapInfo.Load();

			return new Wireup
			{
				Repository = repository,
				SystemSnapshotLoader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), snapshotPath),
				PublicSnapshotRecorder = new PublicSnapshotRecorder(connectionSettings),
				SystemSnapshotRecorder = new SystemSnapshotRecorder(new FileWrapper(), snapshotPath),
				DispatchCheckpointStore = new SqlCheckpointStore(factory, connectionSettings.ConnectionString),
				MessageReceiver = new RabbitChannel(connector, nodeId, x => new RabbitSubscription(x, sourceQueue)),
				MessageSender = new RabbitChannel(connector, nodeId),
				BootstrapInfo = info,
				MessageStore = new SqlMessageStore(factory, connectionSettings.ConnectionString, info.SerializedTypes)
			};
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

			this.MessageReceiver.Dispose();
			DisposeTimeout.Sleep();
		}

		private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(3);
		private bool disposed;
	}
}