namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStore : IMessageStore
	{
		public bool Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return true;

			return false;
		}

		public SqlMessageStore(DbProviderFactory factory, string connectionString)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			this.factory = factory;
			this.connectionString = connectionString;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
	}
}