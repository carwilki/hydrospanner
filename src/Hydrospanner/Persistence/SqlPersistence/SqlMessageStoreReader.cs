namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using Hydrospanner.Phases.Bootstrap;

	public sealed class SqlMessageStoreReader : IEnumerable<JournaledMessage>, IEnumerator<JournaledMessage>
	{
		public bool MoveNext()
		{
			while (true)
			{
				try
				{
					if (this.connection == null)
						this.TryConnect();

					return this.TryRead();
				}
				catch
				{
					this.Dispose();
					Timeout.Sleep();
				}
			}
		}
		private void TryConnect()
		{
			this.connection = this.factory.OpenConnection(this.connectionString);
			this.command = this.connection.CreateCommand(LoadFromSequence.FormatWith(this.startingSequence));
			this.reader = this.command.ExecuteReader();
		}
		private bool TryRead()
		{
			if (this.reader == null)
				return false;

			if (!this.reader.Read())
				return false;

			this.Current = new JournaledMessage
			{
				Sequence = this.currentSequence,
				SerializedType = this.registeredTypes[this.reader.GetInt16(0)],
				ForeignId = this.reader.IsDBNull(1) ? Guid.Empty : this.reader.GetGuid(1),
				SerializedBody = this.reader[2] as byte[],
				SerializedHeaders = this.reader[3] as byte[],
			};

			this.currentSequence++;
			return true;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public IEnumerator<JournaledMessage> GetEnumerator()
		{
			return this;
		}
		public JournaledMessage Current { get; private set; }
		object IEnumerator.Current
		{
			get { return this.Current; }
		}
		public void Reset()
		{
			throw new NotSupportedException();
		}

		public SqlMessageStoreReader(DbProviderFactory factory, string connectionString, IEnumerable<string> types, long startingSequence)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (types == null)
				throw new ArgumentNullException("types");

			if (startingSequence < 0)
				throw new ArgumentOutOfRangeException("startingSequence");

			this.factory = factory;
			this.connectionString = connectionString;
			foreach (var type in types)
				this.registeredTypes[(short)(this.registeredTypes.Count + 1)] = type;
			this.startingSequence = startingSequence;
		}

		public void Dispose()
		{
			this.reader = this.reader.TryDispose();
			this.command = this.command.TryDispose();
			this.connection = this.connection.TryDispose();
		}

		private const string LoadFromSequence = @"SELECT metadata_id, foreign_id, payload, headers FROM messages WHERE sequence >= {0};";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly Dictionary<short, string> registeredTypes = new Dictionary<short, string>(1024);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly long startingSequence;
		private long currentSequence;

		private IDbConnection connection;
		private IDbCommand command;
		private IDataReader reader;
	}
}