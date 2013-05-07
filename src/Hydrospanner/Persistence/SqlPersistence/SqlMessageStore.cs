namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Linq;
	using log4net;
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
				if (this.TrySave(items)) 
					break;
		}
		private bool TrySave(IList<JournalItem> items)
		{
			try
			{
				this.writer.Write(items);
				return true;
			}
			catch (DbException e)
			{
				this.Cleanup();

				if (IsDuplicateViolation(e) && this.IsDuplicateMessage(items.Last()))
					return true;

				Sleep(e);
			}
			catch (Exception e)
			{
				this.Cleanup();
				Sleep(e);
			}

			return false;
		}
		private static bool IsDuplicateViolation(Exception exception)
		{
			var property = exception.GetType().GetProperty("Number");
			return 1062 == (int)property.GetValue(exception, null);
		}
		private bool IsDuplicateMessage(JournalItem last)
		{
			try
			{
				var stored = this.Load(last.MessageSequence).FirstOrDefault();
				return stored != null
				       && stored.Sequence == last.MessageSequence
				       && stored.SerializedType == last.SerializedType
				       && stored.SerializedBody.SequenceEqual(stored.SerializedBody);
			}
			catch
			{
				return false;
			}
		}
		private void Cleanup()
		{
			this.writer.Cleanup();
		}
		private static void Sleep(Exception e)
		{
			Log.Warn("Unable to persist messages to durable storage.", e);
			Timeout.Sleep();
		}

		public SqlMessageStore(
			DbProviderFactory factory,
			string connectionString, 
			SqlMessageStoreWriter writer, 
			JournalMessageTypeRegistrar types)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (writer == null)
				throw new ArgumentNullException("writer");

			if (types == null)
				throw new ArgumentNullException("types");

			this.factory = factory;
			this.connectionString = connectionString;
			this.writer = writer;
			this.types = types;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SqlMessageStore));
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly SqlMessageStoreWriter writer;
		private readonly JournalMessageTypeRegistrar types;
	}
}