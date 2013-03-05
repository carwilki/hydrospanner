namespace Hydrospanner.Outbox
{
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Disruptor;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class SerializationHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.Payload != null || data.Body == null)
				return;

			data.Payload = DefaultEncoding.GetBytes(JsonConvert.SerializeObject(data.Body, Settings));
			data.SerializedHeaders = DefaultEncoding.GetBytes(JsonConvert.SerializeObject(data.Headers, Settings));
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
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