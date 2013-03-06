namespace Hydrospanner
{
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public sealed class DefaultSerializer
	{
		public T Deserialize<T>(byte[] serialized)
		{
			using (var stream = new MemoryStream(serialized))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (var jsonReader = new JsonTextReader(streamReader))
				return this.serializer.Deserialize<T>(jsonReader);
		}
		public byte[] Serialize(object graph)
		{
			using (var stream = new MemoryStream())
			using (var streamWriter = new StreamWriter(stream, DefaultEncoding))
			using (var jsonWriter = new JsonTextWriter(streamWriter))
			{
				this.serializer.Serialize(jsonWriter, graph);
				stream.Position = 0;
				return stream.ToArray();
			}
		}
		private static object AdaptNanoMessageBus(object message)
		{
			return (message as object[]) ?? message;
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