namespace Hydrospanner.Inbox
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
			this.buffer.Add(data);

			if (!this.cache.ContainsKey(data.StreamId))
				this.missingStreams.Add(data.StreamId);

			if (!endOfBatch)
				return;

			// for each stream that is NOT in memory, create a set of hydratables and cache them, then load the stream from disk (and push each event to the next phase)
			// now push each incoming message (in this.buffer) to the next phase

			this.LoadStreams();
			// future: snapshots?
		}
		private void LoadStreams()
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				var builder = new StringBuilder();

				foreach (var item in this.missingStreams)
				{
					builder.AppendFormat("\"{0}\",", item);
					cache[item] = this.factory();
				}

				var value = builder.ToString();
				value = value.Substring(0, value.Length - 1);
				
				command.CommandText = "SELECT * FROM [messages] WHERE stream_id IN ({0});".FormatWith(value);

				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						return;

					while (reader.Read())
					{
						// TODO: claim a sequence and push to the next phase
					}
				}
			}

			this.buffer.Clear();
		}

		public RepositoryHandler(string connectionName, Func<List<IHydratable>> factory)
		{
			this.factory = factory;
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
		}

		private readonly Dictionary<Guid, List<IHydratable>> cache = new Dictionary<Guid, List<IHydratable>>();
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly HashSet<Guid> missingStreams = new HashSet<Guid>();
		private readonly Func<List<IHydratable>> factory;
		private readonly ConnectionStringSettings settings;
	}
}