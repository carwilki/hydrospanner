namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Text;
	using IsolationLevel = System.Data.IsolationLevel;

	internal class PublicSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
			this.catalog.Clear();
			this.saved.Clear();
		}

		public void Record(SnapshotItem item)
		{
			this.catalog[item.Key] = item;
		}

		public void FinishRecording(int generation = 0, long sequence = 0)
		{
			if (!this.catalog.Any())
				return;

			while (true)
			{
				try
				{
					this.SaveSnapshotItems();
					break;
				}
				catch (Exception)
				{
					TimeSpan.FromSeconds(5).Sleep();
				}
			}
		}

		private void SaveSnapshotItems()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				using (var command = connection.CreateCommand())
					while (this.saved.Count < this.catalog.Count)
						this.RecordBatch(command);

				transaction.Commit();
			}
		}

		private void RecordBatch(IDbCommand command)
		{
			var nextBatch = this.DetermineNextBatch();
			this.IncludeNextBatch(command, nextBatch);
			command.ExecuteNonQuery();
			this.saved.AddRange(nextBatch);
		}

		private List<string> DetermineNextBatch()
		{
			var batch = new List<string>();
			var payload = 0;
			foreach (var key in this.catalog.Keys.Skip(this.saved.Count))
			{
				var itemSize = sizeof(int) + sizeof(long) + key.Length + this.catalog[key].Serialized.Length;
				
				if (BatchCapacityReached(itemSize, payload, batch))
					break;

				payload += itemSize;
				batch.Add(key);
			}
			return batch;
		}

		static bool BatchCapacityReached(int nextItem, int alreadyBatched, ICollection batch)
		{
			var payloadCapacityExceeded = nextItem + alreadyBatched > BatchSize;
			var parameterCapacityExceeded = (batch.Count + 1) * ParametersPerStatement >= ParameterLimit;

			return payloadCapacityExceeded || parameterCapacityExceeded;
		}

		private void IncludeNextBatch(IDbCommand command, IList<string> nextBatch)
		{
			command.Parameters.Clear();
			var builder = new StringBuilder(nextBatch.Count * Upsert.Length);

			for (var i = 0; i < nextBatch.Count; i++)
				IncludeItem(command, i, this.catalog[nextBatch[i]], builder);

			command.CommandText = builder.ToString();
		}

		private static void IncludeItem(IDbCommand command, int i, SnapshotItem item, StringBuilder builder)
		{
			command.WithParameter("@id" + i, item.Key, DbType.String);
			command.WithParameter("@sequence" + i, item.CurrentSequence, DbType.Int64);
			command.WithParameter("@hash" + i, item.Serialized.ComputeHash(), DbType.Int32);
			command.WithParameter("@document" + i, item.Serialized, DbType.Binary);
			builder.AppendFormat(Upsert, i);
		}

		public PublicSnapshotRecorder(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}
		
		const int BatchSize = 1024 * 64;
		const string Upsert = @"
			INSERT INTO documents (`identifier`, `message_sequence`, `document_hash`, `document`)
			VALUES ( @id{0}, @sequence{0}, @hash{0}, @document{0} )
			ON DUPLICATE KEY UPDATE `message_sequence` = @sequence{0}, `document_hash` = @hash{0}, `document` = @document{0};";
		const int ParameterLimit = 65000;
		const int ParametersPerStatement = 4;
		readonly ConnectionStringSettings settings;
		readonly IDictionary<string, SnapshotItem> catalog = new Dictionary<string, SnapshotItem>();
		readonly List<string> saved = new List<string>();
	}
}