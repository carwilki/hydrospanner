﻿namespace Hydrospanner.Wireup
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
			return new SqlCheckpointStore(this.factory, this.connectionString);
		}
		public virtual IBootstrapStore CreateBootstrapStore()
		{
			return new SqlBootstrapStore(this.factory, this.connectionString, this.duplicateWindow);
		}
		public virtual IMessageStore CreateMessageStore(IEnumerable<string> journaledTypes)
		{
			var types = new JournalMessageTypeRegistrar(journaledTypes); // TODO: passed in via constructor of this class?
			var session = new SqlBulkInsertSession(this.factory, this.connectionString); // TODO: passed in via constructor of this class?
			var builder = new SqlBulkInsertCommandBuilder(types, session); // TODO: passed in via constructor of this class?
			return new SqlMessageStore(
				this.factory, 
				this.connectionString, 
				() => new SqlMessageStoreWriter(() => session, builder, types), 
				types);
		}

		public PersistenceFactory(string connectionName, int duplicateWindow)
		{
			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			if (duplicateWindow <= 0)
				throw new ArgumentOutOfRangeException("duplicateWindow");

			var settings = ConfigurationManager.ConnectionStrings[connectionName];
			if (settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(connectionName));

			if (string.IsNullOrWhiteSpace(settings.ProviderName) || string.IsNullOrWhiteSpace(settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(connectionName));

			this.factory = DbProviderFactories.GetFactory(settings.ProviderName);
			this.connectionString = settings.ConnectionString;
			this.duplicateWindow = duplicateWindow;
		}
		protected PersistenceFactory()
		{
		}

		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly int duplicateWindow;
	}
}