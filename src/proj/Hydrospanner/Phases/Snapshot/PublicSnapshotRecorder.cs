namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Text;
	using IsolationLevel = System.Data.IsolationLevel;

	internal class PublicSnapshotRecorder : ISnapshotRecorder
	{
		// TODO: integration tests!

		public void StartRecording(int expectedItems)
		{
			this.catalog.Clear();
			this.saved.Clear();
		}

		public void Record(SnapshotItem item)
		{
			this.catalog[item.Key] = item;
		}

		public void FinishRecording(int iteration = 0, long sequence = 0)
		{
			while (true)
			{
				try
				{
					this.SaveSnapshotItems();
				}
				catch (DbException)
				{
					TimeSpan.FromSeconds(5).Sleep();
				}

				// TODO: catch (Exception e) { /* Log the exception... */ }			
			}
		}

		private void SaveSnapshotItems()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				using (var command = connection.CreateCommand())
					while (this.catalog.Count > 0)
						this.ComposeCommand(command).ExecuteNonQuery();

				this.saved.ForEach(x => this.catalog.Remove(x));
				transaction.Commit();
			}
		}

		private IDbCommand ComposeCommand(IDbCommand command)
		{
			command.Parameters.Clear();

			if (this.saved.Count == 0)
				this.saved = this.catalog.Keys.Take(ItemLimit).ToList();
			
			var builder = new StringBuilder(this.saved.Count * Upsert.Length);

			for (var i = 0; i < this.saved.Count; i++)
				IncludeItem(command, i, this.catalog[this.saved[i]], builder);

			command.CommandText = builder.ToString();

			return command;
		}

		private static void IncludeItem(IDbCommand command, int i, SnapshotItem item, StringBuilder builder)
		{
			command.WithParameter("@id" + i, item.Key, DbType.String);
			command.WithParameter("@sequence" + i, item.CurrentSequence, DbType.Int64);
			command.WithParameter("@hash" + i, item.Serialized.ComputeTinyHash(), DbType.Int32);
			command.WithParameter("@document" + i, item.Serialized, DbType.Binary);
			builder.AppendFormat(Upsert, i);
		}

		public PublicSnapshotRecorder(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}

		const int ItemLimit = 500; // ~ 4 unique parameters per Upsert / 2100 (parameter limit)
		const string Upsert = @"
			UPDATE documents SET document = @document{0}, message_sequence = @sequence{0}, document_hash = @hash{0} WHERE identifier = @id{0}; 
			INSERT INTO documents SELECT @id{0}, @sequence{0}, @hash{0}, @document{0} WHERE @@ROWCOUNT = 0;";
		readonly ConnectionStringSettings settings;
		readonly IDictionary<string, SnapshotItem> catalog = new Dictionary<string, SnapshotItem>();
		List<string> saved = new List<string>();
	}
}