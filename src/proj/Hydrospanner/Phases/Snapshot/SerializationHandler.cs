﻿namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;
	using Hydrospanner.Serialization;

	public class SerializationHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			data.Serialize(this.serializer);
		}

		public SerializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly ISerializer serializer;
	}
}