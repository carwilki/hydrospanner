namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data.Common;
	using Persistence;
	using Persistence.SqlPersistence;

	public class PersistenceFactory
	{
		public virtual IDispatchCheckpointStore CreateDispatchCheckpointStore()
		{
			if (this.nullStorage)
				return new NullStore();

			return new SqlCheckpointStore(this.factory, this.connectionString);
		}
		public virtual IBootstrapStore CreateBootstrapStore()
		{
			if (this.nullStorage)
				return new NullStore();

			return new SqlBootstrapStore(this.factory, this.connectionString, this.duplicateWindow);
		}
		public virtual IMessageStore CreateMessageStore(IEnumerable<string> journaledTypes)
		{
			if (this.nullStorage)
				return new NullStore();

			var types = new JournalMessageTypeRegistrar(journaledTypes);
			var session = new SqlBulkInsertSession(this.factory, this.connectionString);
			var builder = new SqlBulkInsertCommandBuilder(types, session);
			var writer = new SqlMessageStoreWriter(session, builder, types, this.maxJournalBatchSize);
			return new SqlMessageStore(this.factory, this.connectionString, writer, types);
		}

		public PersistenceFactory(string connectionName, int duplicateWindow, int maxJournalBatchSize)
		{
			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			if (duplicateWindow <= 0)
				throw new ArgumentOutOfRangeException("duplicateWindow");

			var settings = ConfigurationManager.ConnectionStrings[connectionName];
			if (settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(connectionName));

			this.nullStorage = settings.ConnectionString == "null-storage";
			if (this.nullStorage)
				return;

			if (string.IsNullOrWhiteSpace(settings.ProviderName) || string.IsNullOrWhiteSpace(settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(connectionName));

			if (maxJournalBatchSize < 10)
				throw new ArgumentOutOfRangeException("maxJournalBatchSize");

			this.factory = DbProviderFactories.GetFactory(settings.ProviderName);
			this.connectionString = settings.ConnectionString;
			this.duplicateWindow = duplicateWindow;
			this.maxJournalBatchSize = maxJournalBatchSize;
		}
		protected PersistenceFactory()
		{
		}

		private readonly bool nullStorage;
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly int duplicateWindow;
		readonly int maxJournalBatchSize;
	}
}