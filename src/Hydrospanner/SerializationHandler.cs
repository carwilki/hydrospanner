namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public class SerializationHandler : IEventHandler<WireMessage>, IEventHandler<TransformationMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.Body == null && data.SerializedBody != null)
				data.Body = this.serializer.Deserialize<object>(data.SerializedBody);

			if (data.WireId != Guid.Empty)
				data.Body = (data.Body as object[]) ?? data.Body; // adapt NanoMessageBus

			if (data.Body != null && data.SerializedBody == null)
				data.SerializedBody = this.serializer.Serialize(data.Body);

			if (data.Headers == null && data.SerializedHeaders != null)
				data.Headers = this.serializer.Deserialize<Dictionary<string, string>>(data.SerializedHeaders) ?? new Dictionary<string, string>();
			else if (data.Headers != null && data.SerializedHeaders == null)
				data.SerializedHeaders = this.serializer.Serialize(data.Headers);
		}

		public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
			if (data.Body == null && data.SerializedBody != null)
				data.Body = this.serializer.Deserialize<object>(data.SerializedBody);

			if (data.Headers == null && data.SerializedHeaders != null)
				data.Headers = this.serializer.Deserialize<Dictionary<string, string>>(data.SerializedHeaders) ?? new Dictionary<string, string>();
		}

		private readonly DefaultSerializer serializer = new DefaultSerializer();
	}
}