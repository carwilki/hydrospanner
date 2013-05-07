namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Linq;
	using System.Text;
	using log4net;
	using IsolationLevel = System.Data.IsolationLevel;

	internal class PublicSnapshotRecorder : ISnapshotRecorder
	{
		public void StartRecording(int expectedItems)
		{
			this.catalog.Clear();
			this.saved = 0;
		}
		public void Record(SnapshotItem item)
		{
			this.catalog[item.Key] = item;
		}
		public void FinishRecording(long sequence = 0)
		{
			if (this.catalog.Count == 0)
				return;

			while (true)
			{
				try
				{
					this.SaveSnapshotItems();
					break;
				}
				catch (Exception e)
				{
					Log.Warn("Unable to persist to database.", e);
					SleepTimeout.Sleep();
				}
			}
		}

		private void SaveSnapshotItems()
		{
			var keys = this.catalog.Keys.ToArray();

			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				using (var command = connection.CreateCommand())
					while (this.saved < keys.Length)
						this.RecordBatch(command, keys);

				transaction.Commit();
			}
		}
		private void RecordBatch(IDbCommand command, string[] keys)
		{
			this.currentBatch.Clear();
			this.AppendKeysToNextBatch(keys);
			this.IncludeNextBatch(command);
			command.ExecuteNonQuery();
			this.saved += this.currentBatch.Count;
		}
		private void AppendKeysToNextBatch(string[] keys)
		{
			var payloadSize = 0;

			for (var i = this.saved; i < keys.Length; i++)
			{
				var key = keys[i];
				var item = this.catalog[key];

				var serialized = item.Serialized;
				var itemSize = sizeof(int) + sizeof(long) + key.Length + (serialized == null ? 0 : serialized.Length);
				if (BatchCapacityReached(itemSize, payloadSize, this.currentBatch.Count))
					break;

				payloadSize += itemSize;
				this.currentBatch.Add(item);
			}
		}
		private static bool BatchCapacityReached(int nextItem, int alreadyBatched, int batchCount)
		{
			var payloadCapacityExceeded = nextItem + alreadyBatched > BatchSize;
			var parameterCapacityExceeded = (batchCount + 1) * ParametersPerStatement >= ParameterLimit;

			return payloadCapacityExceeded || parameterCapacityExceeded;
		}
		private void IncludeNextBatch(IDbCommand command)
		{
			command.Parameters.Clear();
			this.builder.Clear();

			for (var i = 0; i < this.currentBatch.Count; i++)
				this.IncludeItem(command, i, this.currentBatch[i]);

			command.CommandText = this.builder.ToString();
		}
		private void IncludeItem(IDbCommand command, int i, SnapshotItem item)
		{
			command.WithParameter("@i" + i, item.Key, DbType.String);
			command.WithParameter("@s" + i, item.CurrentSequence, DbType.Int64);
			command.WithParameter("@h" + i, item.Serialized.ComputeHash(), DbType.UInt32);
			command.WithParameter("@d" + i, item.Serialized, DbType.Binary);
			this.builder.AppendFormat(Upsert, i);
		}

		public PublicSnapshotRecorder(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}
		
		private const int BatchSize = 1024 * 64;
		private const string Upsert = "INSERT INTO documents VALUES (@i{0}, @s{0}, @h{0}, @d{0}) ON DUPLICATE KEY UPDATE sequence = @s{0}, hash = @h{0}, document = @d{0};";
		private const int ParameterLimit = 65000;
		private const int ParametersPerStatement = 4;
		private static readonly ILog Log = LogManager.GetLogger(typeof(PublicSnapshotRecorder));
		private static readonly TimeSpan SleepTimeout = TimeSpan.FromSeconds(5);
		private readonly ConnectionStringSettings settings;
		private readonly IDictionary<string, SnapshotItem> catalog = new Dictionary<string, SnapshotItem>();
		private readonly StringBuilder builder = new StringBuilder(1024 * 1024);
		private readonly List<SnapshotItem> currentBatch = new List<SnapshotItem>(BatchSize); 
		private int saved;
	}
}