namespace Hydrospanner
{
	using System.Collections.Generic;
	using Disruptor;

	public sealed class SerializationHandler : IEventHandler<WireMessage>, IEventHandler<DispatchMessage>, IEventHandler<SnapshotMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// TODO: how to deserialize NanoMessageBus-published events?
			if (data.Body == null)
				data.Body = this.serializer.Deserialize(data.SerializedBody, data.SerializedType);

			if (data.Headers == null)
				data.Headers = this.serializer.Deserialize<Dictionary<string, string>>(data.SerializedHeaders) ?? new Dictionary<string, string>();
		}

		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.SerializedBody == null)
			{
				data.SerializedBody = this.serializer.Serialize(data.Body);
				data.SerializedType = data.Body.GetType().FullName;
			}

			if (data.SerializedHeaders == null && data.Headers != null && data.Headers.Count > 0)
				data.SerializedHeaders = this.serializer.Serialize(data.Headers);
		}
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			data.Serialized = this.serializer.Serialize(data.Memento);
		}

		private readonly DefaultSerializer serializer = new DefaultSerializer();
	}
}