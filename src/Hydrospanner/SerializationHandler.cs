namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public sealed class SerializationHandler : IEventHandler<WireMessage>, IEventHandler<DispatchMessage>, IEventHandler<SnapshotMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.Body == null)
				data.Body = this.serializer.Deserialize<object>(data.SerializedBody);

			if (data.Headers == null)
				data.Headers = this.serializer.Deserialize<Dictionary<string, string>>(data.SerializedHeaders) ?? new Dictionary<string, string>();

			if (data.WireId != Guid.Empty)
				data.Body = (data.Body as object[]) ?? data.Body; // adapt NanoMessageBus if necessary
		}

		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.SerializedBody == null)
				data.SerializedBody = this.serializer.Serialize(data.Body);

			if (data.SerializedHeaders == null)
				data.SerializedHeaders = this.serializer.Serialize(data.Headers);
		}
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			data.Serialized = this.serializer.Serialize(data.Memento);
		}

		private readonly DefaultSerializer serializer = new DefaultSerializer();
	}
}