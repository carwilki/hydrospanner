namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using Phases.Journal;

	public class SqlMessageStoreWriter
	{
		public virtual void Write(IList<JournalItem> items)
		{
			this.session.BeginTransaction();
			this.SaveInSlices(items);
			this.session.CommitTransaction();
			this.types.MarkPendingAsRegistered();
			this.Cleanup();
		}

		private void SaveInSlices(IList<JournalItem> items)
		{
			var slices = this.CountSlices(items);
			for (var slice = 0; slice < slices; slice++)
			{
				this.session.PrepareNewCommand();
				var commandText = this.BuildCommand(items, slice);
				this.session.ExecuteCurrentCommand(commandText);
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

		public virtual void Cleanup()
		{
			this.builder.Cleanup();
			this.session.Cleanup();
		}

		public SqlMessageStoreWriter(
			SqlBulkInsertSession session,
			SqlBulkInsertCommandBuilder builder,
			JournalMessageTypeRegistrar types,
			int maxSliceSize)
		{
			if (session == null)
				throw new ArgumentNullException("session");

			if (builder == null)
				throw new ArgumentNullException("builder");

			if (types == null)
				throw new ArgumentNullException("types");

			if (maxSliceSize < 10)
				throw new ArgumentOutOfRangeException("maxSliceSize");

			this.session = session;
			this.builder = builder;
			this.types = types;
			this.maxSliceSize = maxSliceSize;
		}

		protected SqlMessageStoreWriter()
		{
		}

		private readonly SqlBulkInsertSession session;
		private readonly SqlBulkInsertCommandBuilder builder;
		private readonly JournalMessageTypeRegistrar types;
		private readonly int maxSliceSize;
	}
}