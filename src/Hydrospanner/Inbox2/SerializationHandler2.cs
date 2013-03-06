﻿namespace Hydrospanner.Inbox2
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public class SerializationHandler2 : IEventHandler<WireMessage2>
	{
		public void OnNext(WireMessage2 data, long sequence, bool endOfBatch)
		{
			if (data.Body == null && data.SerializedBody != null)
				data.Body = this.serializer.Deserialize<object>(data.SerializedBody);

			if (data.WireId != Guid.Empty)
				data.Body = (data.Body as object[]) ?? data.Body; // adapt NanoMessageBus

			if (data.Body != null && data.SerializedBody == null)
				data.SerializedBody = this.serializer.Serialize(data.Body);

			if (data.Headers == null && data.SerializedHeaders != null)
				data.Headers = this.serializer.Deserialize<Dictionary<string, string>>(data.SerializedHeaders);
			else if (data.Headers != null && data.SerializedHeaders == null)
				data.SerializedHeaders = this.serializer.Serialize(data.Headers);
		}

		private readonly DefaultSerializer serializer = new DefaultSerializer();
	}
}