namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Linq;
	using Phases.Journal;
	using Wireup;

	public sealed class SqlMessageStore : IMessageStore
	{
		public IEnumerable<JournaledMessage> Load(long startingSequence)
		{
			return new SqlMessageStoreReader(this.factory, this.connectionString, this.types.AllTypes.ToList(), startingSequence);
		}

		public void Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return;

			while (true)
			{
				var writer = new SqlMessageStoreWriter(this.factory, this.connectionString, this.builder, this.types);
				try
				{
					writer.TryWrite(items);
					break;
				}
				catch
				{
					Timeout.Sleep();
				}
				finally
				{
					writer.Cleanup();
				}
			}
		}

		public SqlMessageStore(
			DbProviderFactory factory,
			string connectionString, 
			BulkMessageInsertBuilder builder, 
			JournalMessageTypeRegistrar types)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			// TODO: more null checks?

			this.factory = factory;
			this.connectionString = connectionString;
			this.builder = builder;
			this.types = types;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly BulkMessageInsertBuilder builder;
		private readonly JournalMessageTypeRegistrar types;
	}
}