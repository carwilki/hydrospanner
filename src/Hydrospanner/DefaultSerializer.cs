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
			using (new JsonTextReader(streamReader))
				return this.serializer.Deserialize<T>(new JsonTextReader(streamReader));
		}
		public byte[] Serialize(object graph)
		{
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream, DefaultEncoding))
			{
				this.serializer.Serialize(writer, graph);
				stream.Position = 0;
				return stream.ToArray();
			}
		}
		private static object AdaptNanoMessageBus(object message)
		{
			return (message as object[]) ?? message;
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
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