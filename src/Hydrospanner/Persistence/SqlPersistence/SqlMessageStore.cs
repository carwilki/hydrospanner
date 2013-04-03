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
				try
				{
					using (var writer = this.writerFactory())
					{
						writer.TryWrite(items);
						break;
					}
				}
				catch
				{
					Timeout.Sleep();
				}
			}
		}

		public SqlMessageStore(
			DbProviderFactory factory,
			string connectionString, 
			Func<SqlMessageStoreWriter> writerFactory, 
			JournalMessageTypeRegistrar types)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (writerFactory == null)
				throw new ArgumentNullException("writerFactory");

			if (types == null)
				throw new ArgumentNullException("types");

			this.factory = factory;
			this.connectionString = connectionString;
			this.writerFactory = writerFactory;
			this.types = types;
		}

		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly Func<SqlMessageStoreWriter> writerFactory;
		private readonly JournalMessageTypeRegistrar types;
	}
}