namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using Phases.Journal;

	public class SqlMessageStoreWriter : IDisposable
	{
		public virtual void Write(IList<JournalItem> items)
		{
			using (var session = this.sessionFactory())
			{
				session.BeginTransaction();
				this.SaveInSlices(session, items);
				session.CommitTransaction();
				this.types.MarkPendingAsRegistered();
			}
		}

		private void SaveInSlices(SqlBulkInsertSession session, IList<JournalItem> items)
		{
			var slices = this.CountSlices(items);
			for (var slice = 0; slice < slices; slice++)
			{
				session.PrepareNewCommand();
				var commandText = this.BuildCommand(items, slice);
				session.ExecuteCurrentCommand(commandText);
			}
		}

		private int CountSlices(ICollection<JournalItem> items)
		{
			int remainder;
			var slices = Math.DivRem(items.Count, this.maxSliceSize, out remainder);
			return slices + Math.Sign(remainder);
		}

		private string BuildCommand(IList<JournalItem> items, int sliceIndex)
		{
			var start = sliceIndex * this.maxSliceSize;
			var finish = Math.Min(items.Count, start + this.maxSliceSize);
			var sliceSize = finish - start;

			this.builder.NewBatch();

			for (var i = 0; i < sliceSize; i++)
				this.builder.Include(items[start + i]);

			return this.builder.Build();
		}

		public SqlMessageStoreWriter(
			Func<SqlBulkInsertSession> sessionFactory,
			SqlBulkInsertCommandBuilder builder,
			JournalMessageTypeRegistrar types,
			int maxSliceSize)
		{
			if (sessionFactory == null)
				throw new ArgumentNullException("sessionFactory");

			if (builder == null)
				throw new ArgumentNullException("builder");

			if (types == null)
				throw new ArgumentNullException("types");

			if (maxSliceSize < 10)
				throw new ArgumentOutOfRangeException("maxSliceSize");

			this.sessionFactory = sessionFactory;
			this.builder = builder;
			this.types = types;
			this.maxSliceSize = maxSliceSize;
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

		private readonly Func<SqlBulkInsertSession> sessionFactory;
		private readonly SqlBulkInsertCommandBuilder builder;
		private readonly JournalMessageTypeRegistrar types;
		private readonly int maxSliceSize;
	}
}