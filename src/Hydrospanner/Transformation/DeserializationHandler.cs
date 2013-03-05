namespace Hydrospanner.Transformation
{
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Disruptor;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class DeserializationHandler : IEventHandler<TransformationMessage>
	{
		public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
			if (data.Body != null || data.Payload == null)
				return; // already deserialized || nothing to deserialize

			using (var stream = new MemoryStream(data.Payload))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (new JsonTextReader(streamReader))
				data.Body = this.serializer.Deserialize(new JsonTextReader(streamReader));

			// deserialize anything that hasn't already been deserialized.
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