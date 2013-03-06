namespace Hydrospanner.Inbox2
{
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Disruptor;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class SerializationHandler2 : IEventHandler<IMessage>
	{
		public void OnNext(IMessage data, long sequence, bool endOfBatch)
		{
			if (data.Body == null && data.SerializedBody != null)
				using (var stream = new MemoryStream(data.SerializedBody))
					data.Body = this.Deserialize<object>(stream);

			if (data.Body != null)
				AdaptNanoMessageBus(data);

			if (data.Body != null && data.SerializedBody == null)
				data.SerializedBody = this.Serialize(data.Body);

			if (data.Headers == null && data.SerializedHeaders != null)
				using (var stream = new MemoryStream(data.SerializedHeaders))
					data.Body = this.Deserialize<Dictionary<string, string>>(stream);
			
			else if (data.Headers != null && data.SerializedHeaders == null)
				data.SerializedHeaders = this.Serialize(data.Headers);
		}
		private T Deserialize<T>(Stream stream)
		{
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (new JsonTextReader(streamReader))
				return this.serializer.Deserialize<T>(new JsonTextReader(streamReader));
		}
		private byte[] Serialize(object graph)
		{
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream, DefaultEncoding))
			{
				this.serializer.Serialize(writer, graph);
				stream.Position = 0;
				return stream.ToArray();
			}
		}
		private static void AdaptNanoMessageBus(IMessage data)
		{
			var body = data.Body as object[];
			if (body != null && body.Length > 0)
				data.Body = body[0];
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly JsonSerializer serializer = new JsonSerializer
		{
			TypeNameHandling = TypeNameHandling.All,
			TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			Converters = { new StringEnumConverter() }
		};
	}
}