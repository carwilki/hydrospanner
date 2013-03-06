namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Text;
	using Disruptor;

	public class RepositoryHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.DuplicateMessage)
				return;

			if (!this.cache.ContainsKey(data.StreamId))
				this.missingStreams.Add(data.StreamId);

			this.buffer.Add(data);
			if (!endOfBatch)
				return;

			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT stream_id, payload, headers FROM messages WHERE sequence <= (SELECT sequence FROM checkpoints) AND stream_id IN ({0});".FormatWith(this.GetUncachedStreamIdentifiers());
				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						return;

					while (reader.Read())
					{
						var claimed = this.ring.Next();
						var message = this.ring[claimed];

						var streamId = reader.GetGuid(0);
						message.Hydratables = this.cache[streamId];
						message.SerializedBody = reader[1] as byte[];
						message.SerializedHeaders = reader[2] as byte[];
						message.Replay = true;

						this.ring.Publish(claimed);
					}
				}
			}

			foreach (var item in this.buffer)
			{
				var claimed = this.ring.Next();
				var message = this.ring[claimed];
				message.Body = item.Body;
				message.Headers = item.Headers;
				message.MessageSequence = item.MessageSequence;
				message.Hydratables = this.cache[item.StreamId];
				message.Replay = false;

				this.ring.Publish(claimed);
			}

			this.buffer.Clear();
		}
		private string GetUncachedStreamIdentifiers()
		{
			if (this.missingStreams.Count == 0)
				return string.Empty;

			var builder = new StringBuilder();

			foreach (var stream in this.missingStreams)
			{
				builder.AppendFormat("'{0}',", stream);
				this.cache[stream] = this.factory(stream);
			}

			var identifiers = builder.ToString();
			return identifiers.Substring(0, identifiers.Length - 1);
		}

		public RepositoryHandler(RingBuffer<TransformationMessage> ring, string connectionName, Func<Guid, IHydratable[]> factory)
		{
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
			this.factory = factory;
			this.ring = ring;
		}

		private readonly Dictionary<Guid, IHydratable[]> cache = new Dictionary<Guid, IHydratable[]>(); // TODO: MRU
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly HashSet<Guid> missingStreams = new HashSet<Guid>();
		private readonly RingBuffer<TransformationMessage> ring;
		private readonly ConnectionStringSettings settings;
		private readonly Func<Guid, IHydratable[]> factory;
	}
}