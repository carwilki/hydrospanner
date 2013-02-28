namespace Hydrospanner
{
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Disruptor;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class DeserializationHandler : IEventHandler<ReceivedMessage>
	{
		public void OnNext(ReceivedMessage data, long sequence, bool endOfBatch)
		{
			var headers = data.Headers;
			foreach (var key in headers.Keys)
				headers[key] = Encoding.UTF8.GetString(headers[key] as byte[] ?? new byte[0]);

			using (var stream = new MemoryStream(data.RawBody))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (new JsonTextReader(streamReader))
				data.Body = this.serializer.Deserialize(new JsonTextReader(streamReader));
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