namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;
	using JsonNetSerializer = Newtonsoft.Json.JsonSerializer;

	public class JsonSerializer : ISerializer
	{
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return null;

			try
			{
				using (var stream = new MemoryStream())
				using (var streamWriter = new StreamWriter(stream, DefaultEncoding))
				using (var jsonWriter = new JsonTextWriter(streamWriter))
				{
					this.serializer.Serialize(jsonWriter, graph);
					jsonWriter.Flush();
					return stream.ToArray();
				}
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}

		public T Deserialize<T>(byte[] serialized)
		{
			if (serialized == null || serialized.Length == 0)
				return default(T);

			try
			{
				using (var stream = new MemoryStream(serialized))
				using (var streamReader = new StreamReader(stream, DefaultEncoding))
				using (var jsonReader = new JsonTextReader(streamReader))
					return this.serializer.Deserialize<T>(jsonReader);
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}
		public object Deserialize(byte[] serialized, string typeName)
		{
			if (serialized == null || serialized.Length == 0)
				return null;

			var type = this.LoadType(typeName);
			if (type == null)
				throw new SerializationException("Type '{0}' not found.".FormatWith(typeName));

			try
			{
				using (var stream = new MemoryStream(serialized))
				using (var streamReader = new StreamReader(stream, DefaultEncoding))
				using (var jsonReader = new JsonTextReader(streamReader))
					return this.serializer.Deserialize(jsonReader, type);
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}
		private Type LoadType(string typeName)
		{
			Type type = null;
			if (!string.IsNullOrEmpty(typeName) && !this.types.TryGetValue(typeName, out type))
				this.types[typeName] = type = Type.GetType(typeName);

			return type;
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>(1024);
		private readonly JsonNetSerializer serializer = new JsonNetSerializer
		{
			NullValueHandling = NullValueHandling.Ignore,
			TypeNameHandling = TypeNameHandling.None,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			Converters = { new UnderscoreEnumConverter(), new StringEnumConverter() },
			ContractResolver = new UnderscoreContractResolver()
		};
	}
}