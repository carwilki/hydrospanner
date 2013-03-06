namespace Hydrospanner.Inbox
{
	using System;
	using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Text;
    using Disruptor;
    using Transformation;

	public class RepositoryHandler : IEventHandler<WireMessage>
	{
	    public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			this.buffer.Add(data);

			if (!this.cache.ContainsKey(data.StreamId))
				this.missingStreams.Add(data.StreamId);

			if (!endOfBatch)
				return;

			this.LoadStreams();
            this.buffer.Clear();
            this.missingStreams.Clear();
		}

		private void LoadStreams()
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
			    command.CommandText = this.BuildLoadStreamsCommand();
			    if (string.IsNullOrWhiteSpace(command.CommandText))
			        return;

				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						return;

				    var streamLengths = this.GatherStreamLengths(reader);

				    this.PublishReplayMessages(reader, streamLengths);
				}
			}
		}

	    private string BuildLoadStreamsCommand()
	    {
	        var streamIdBuilder = new StringBuilder();
	        var streamIndexBuilder = new StringBuilder();

	        foreach (var item in this.missingStreams)
	        {
	            streamIndexBuilder.AppendFormat(CountStream, item);
	            streamIdBuilder.AppendFormat("'{0}',", item);
	            this.cache[item] = this.factory();
	        }

	        var streamIds = streamIdBuilder.ToString();
	        if (streamIds.Length == 0)
	            return null;

	        streamIds = streamIds.Substring(0, streamIds.Length - 1);

	        return LoadStream.FormatWith(streamIndexBuilder.ToString(), streamIds);
	    }

	    private Dictionary<Guid, long> GatherStreamLengths(IDataReader reader)
	    {
	        var streamLengths = new Dictionary<Guid, long>();
	        foreach (var streamId in this.missingStreams)
	        {
	            reader.Read();
	            streamLengths[streamId] = reader.GetInt64(0);
	            reader.NextResult();
	        }
	        return streamLengths;
	    }

	    private void PublishReplayMessages(IDataReader reader, IDictionary<Guid, long> streamLengths)
	    {
            var streamIndices = this.missingStreams.ToDictionary(x => x, y => 0);

	        while (reader.Read())
	        {
	            var claimed = this.transformationPhase.Next();
	            var message = this.transformationPhase[claimed];
	            var sequence = reader.GetInt64(0);
	            var streamId = reader.GetGuid(1);
	            var payload = (byte[])reader[2];
	            var headers = (byte[])reader[3];

	            message.Payload = payload;
	            message.Headers = headers;
	            message.Hydratables = this.cache[streamId].ToArray();
	            message.IncomingSequence = sequence;
	            message.StreamId = streamId;
	            message.StreamIndex = ++streamIndices[streamId];
	            message.StreamLength = streamLengths[streamId];

	            this.transformationPhase.Publish(claimed);
	        }
	    }

	    public RepositoryHandler(
            string connectionName, Func<List<IHydratable<object>>> factory, RingBuffer<TransformationMessage> transformationPhase)
		{
			this.factory = factory;
	        this.settings = ConfigurationManager.ConnectionStrings[connectionName];
	        this.transformationPhase = transformationPhase;
		}

	    private const string CountStream = @"
            SELECT COUNT_BIG(*)
              FROM [messages]
             WHERE stream_id = '{0}';";
        private const string LoadStream = @"
            {0}

            SELECT sequence, stream_id, payload, headers 
              FROM [messages] 
             WHERE stream_id IN ({1});";
        private readonly Dictionary<Guid, List<IHydratable<object>>> cache = new Dictionary<Guid, List<IHydratable<object>>>();
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly HashSet<Guid> missingStreams = new HashSet<Guid>();
		private readonly Func<List<IHydratable<object>>> factory;
	    private readonly ConnectionStringSettings settings;
	    private readonly RingBuffer<TransformationMessage> transformationPhase;
	}
}