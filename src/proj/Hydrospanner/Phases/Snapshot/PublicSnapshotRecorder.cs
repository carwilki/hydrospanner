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
			this.TrySaveSnapshot();
		}

		void TrySaveSnapshot()
		{
			while (true)
			{
				try
				{
					this.SaveSnapshotItems();
					return;
				}
				catch (DbException)
				{
					TimeSpan.FromSeconds(5).Sleep();
				}
				//// TODO: catch (Exception e) { /* Log the exception... */ }
			}
		}

		private void SaveSnapshotItems()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				while (this.catalog.Count > 0)
				{
					using (var command = connection.CreateCommand())
					{
						foreach (var s in this.ComposeCommand(command))
							this.saved.Add(s);

						command.ExecuteNonQuery();
					
						foreach (var s in this.saved)
							this.catalog.Remove(s);
					}
				}
				transaction.Commit();
				this.catalog.Clear();
				this.saved.Clear();
			}
		}

		private IEnumerable<string> ComposeCommand(IDbCommand command)
		{
			IList<string> items;
			if (this.saved.Count > 0)
				items = this.saved.ToList();
			else
				items = this.catalog.Keys.Take(ItemLimit).ToList();
			
			var builder = new StringBuilder(items.Count * Upsert.Length);

			for (var i = 0; i < items.Count; i++)
				IncludeItem(command, i, this.catalog[items[i]], builder);

			command.CommandText = builder.ToString();
			return items;
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
		readonly HashSet<string> saved = new HashSet<string>();
	}
}