namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using Phases.Journal;

	public class SqlMessageStoreWriter
	{
		public void TryWrite(IList<JournalItem> items)
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				this.SaveInSlices(items, transaction);
				transaction.Commit();
				types.MarkPendingAsRegistered();
			}
		}

		void SaveInSlices(IList<JournalItem> items, IDbTransaction transaction)
		{
			var slices = CountSlices(items);
			for (var slice = 0; slice < slices; slice++)
				using (var command = this.BuildCommand(items, slice, transaction))
					TryExecute(command);
		}

		static int CountSlices(ICollection<JournalItem> items)
		{
			var slices = items.Count / MaxSliceSize;
			return items.Count % MaxSliceSize != 0 ? slices + 1 : slices;
		}

		IDbCommand BuildCommand(IList<JournalItem> items, int sliceIndex, IDbTransaction transaction)
		{
			var start = sliceIndex*MaxSliceSize;
			var finish = Math.Min(items.Count, start + MaxSliceSize);
			var sliceSize = finish - start;

			this.builder.NewInsert(transaction);

			for (var i = 0; i < sliceSize; i++)
				this.builder.Include(items[start + i]);

			return this.builder.Build();
		}

		static void TryExecute(IDbCommand command)
		{
			try
			{
				command.ExecuteNonQuery();
			}
			catch (Exception)
			{
				throw;
			}
		}

		public void Cleanup()
		{
			this.builder.Cleanup();
		}

		public SqlMessageStoreWriter(
			DbProviderFactory factory, 
			string connectionString, 
			BulkMessageInsertBuilder builder, 
			JournalMessageTypeRegistrar types)
		{
			// TODO: null checks

			this.factory = factory;
			this.connectionString = connectionString;
			this.builder = builder;
			this.types = types;
		}

		private const int MaxSliceSize = 5000;
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly BulkMessageInsertBuilder builder;
		private readonly JournalMessageTypeRegistrar types;
	}
}