namespace Hydrospanner.Inbox
{
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Disruptor;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class InboxDeserializationHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			var headers = data.Headers;
			foreach (var key in headers.Keys)
				headers[key] = Encoding.UTF8.GetString(headers[key] as byte[] ?? new byte[0]);

			using (var stream = new MemoryStream(data.Payload))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (new JsonTextReader(streamReader))
				data.Body = this.serializer.Deserialize(new JsonTextReader(streamReader));

			// NanoMessageBus adapter to remove the object[] channel envelope...
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