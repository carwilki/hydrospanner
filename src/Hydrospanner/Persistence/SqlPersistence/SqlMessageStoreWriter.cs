namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using Phases.Journal;

	public class SqlMessageStoreWriter : IDisposable
	{
		public virtual void TryWrite(IList<JournalItem> items)
		{
			using (var session = this.sessionFactory())
			{
				session.BeginNewSession();
				this.SaveInSlices(session, items);
				session.CommitTransaction();
				types.MarkPendingAsRegistered();
			}
		}

		void SaveInSlices(SqlBulkInsertSession session, IList<JournalItem> items)
		{
			var slices = CountSlices(items);
			for (var slice = 0; slice < slices; slice++)
			{
				session.PrepareNewCommand();
				var commandText = this.BuildCommand(items, slice);
				session.ExecuteCurrentCommand(commandText);
			}
		}

		static int CountSlices(ICollection<JournalItem> items)
		{
			int remainder;
			var slices = Math.DivRem(items.Count, MaxSliceSize, out remainder);
			return slices + Math.Sign(remainder);
		}

		string BuildCommand(IList<JournalItem> items, int sliceIndex)
		{
			var start = sliceIndex * MaxSliceSize;
			var finish = Math.Min(items.Count, start + MaxSliceSize);
			var sliceSize = finish - start;

			this.builder.NewInsert();

			for (var i = 0; i < sliceSize; i++)
				this.builder.Include(items[start + i]);

			return this.builder.Build();
		}

		public SqlMessageStoreWriter(
			Func<SqlBulkInsertSession> sessionFactory,
			SqlBulkInsertCommandBuilder builder, 
			JournalMessageTypeRegistrar types)
		{
			// TODO: null checks

			this.sessionFactory = sessionFactory;
			this.builder = builder;
			this.types = types;
		}
		protected SqlMessageStoreWriter()
		{
		}
		public virtual void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.builder.Cleanup();
		}

		private const int MaxSliceSize = 5000;
		private readonly SqlBulkInsertCommandBuilder builder;
		private readonly JournalMessageTypeRegistrar types;
		private readonly Func<SqlBulkInsertSession> sessionFactory;
	}
}