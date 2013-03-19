namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStore : IMessageStore
	{
		public void Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return;

			while (true)
			{
				try
				{
					this.TrySave(items);
				}
				catch
				{
					Timeout.Sleep();
				}
			}
		}
		private void TrySave(List<JournalItem> items)
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var tranaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				tranaction.Commit();
			}
		}

		private int RegisterType(string type)
		{
			int id;
			if (!this.registeredTypes.TryGetValue(type, out id))
				this.registeredTypes[type] = id = this.registeredTypes.Count + 1;

			return id;
		}

		public SqlMessageStore(DbProviderFactory factory, string connectionString, IEnumerable<string> types)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (types == null)
				throw new ArgumentNullException("types");

			this.factory = factory;
			this.connectionString = connectionString;
			foreach (var type in types)
				this.RegisterType(type);
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly Dictionary<string, int> registeredTypes = new Dictionary<string, int>(1024);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
	}
}