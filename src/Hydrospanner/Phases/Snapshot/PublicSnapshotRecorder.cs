﻿namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Linq;
	using System.Text;
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
				catch (Exception)
				{
					SleepTimeout.Sleep();
				}
			}
		}

		private void SaveSnapshotItems()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				var keys = this.catalog.Keys.ToArray();
				using (var command = connection.CreateCommand())
					while (this.saved < this.catalog.Count)
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
			var payload = 0;

			for (var i = this.saved; i < keys.Length; i++)
			{
				var key = keys[i];

				var itemSize = sizeof(int) + sizeof(long) + key.Length + this.catalog[key].Serialized.Length;

				if (BatchCapacityReached(itemSize, payload, this.currentBatch))
					break;

				payload += itemSize;
				this.currentBatch.Add(key);
			}
		}
		private static bool BatchCapacityReached(int nextItem, int alreadyBatched, ICollection batch)
		{
			var payloadCapacityExceeded = nextItem + alreadyBatched > BatchSize;
			var parameterCapacityExceeded = (batch.Count + 1) * ParametersPerStatement >= ParameterLimit;

			return payloadCapacityExceeded || parameterCapacityExceeded;
		}
		private void IncludeNextBatch(IDbCommand command)
		{
			command.Parameters.Clear();
			this.builder.Clear();

			for (var i = 0; i < this.currentBatch.Count; i++)
				this.IncludeItem(command, i, this.catalog[this.currentBatch[i]]);

			command.CommandText = this.builder.ToString();
		}
		private void IncludeItem(IDbCommand command, int i, SnapshotItem item)
		{
			command.WithParameter("@id" + i, item.Key, DbType.String);
			command.WithParameter("@sequence" + i, item.CurrentSequence, DbType.Int64);
			command.WithParameter("@hash" + i, item.Serialized.ComputeHash(), DbType.UInt32);
			command.WithParameter("@document" + i, item.Serialized, DbType.Binary);
			this.builder.AppendFormat(Upsert, i);
		}

		public PublicSnapshotRecorder(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}
		
		private const int BatchSize = 1024 * 64;
		private const string Upsert = @"
			INSERT INTO documents (identifier, message_sequence, document_hash, document)
			VALUES (@id{0}, @sequence{0}, @hash{0}, @document{0})
			ON DUPLICATE KEY UPDATE message_sequence = @sequence{0}, document_hash = @hash{0}, document = @document{0};";
		private const int ParameterLimit = 65000;
		private const int ParametersPerStatement = 4;
		private static readonly TimeSpan SleepTimeout = TimeSpan.FromSeconds(5);
		private readonly ConnectionStringSettings settings;
		private readonly IDictionary<string, SnapshotItem> catalog = new Dictionary<string, SnapshotItem>();
		private readonly StringBuilder builder = new StringBuilder(1024 * 1024);
		private readonly List<string> currentBatch = new List<string>(BatchSize); 
		private int saved;
	}
}