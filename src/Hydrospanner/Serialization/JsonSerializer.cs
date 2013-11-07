namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

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
			Type deserialized;
			return this.Deserialize(serialized, typeName, out deserialized);
		}
		public object Deserialize(byte[] serialized, string typeName, out Type deserializedType)
		{
			deserializedType = null;
			if (serialized == null || serialized.Length == 0)
				return null;

			deserializedType = this.LoadType(typeName);
			if (deserializedType == null)
				throw new SerializationException("Type '{0}' not found.".FormatWith(typeName));

			try
			{
				using (var stream = new MemoryStream(serialized))
				using (var streamReader = new StreamReader(stream, DefaultEncoding))
				using (var jsonReader = new JsonTextReader(streamReader))
					return this.serializer.Deserialize(jsonReader, deserializedType);
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

		public JsonSerializer(IEnumerable<KeyValuePair<string, Type>> aliases = null)
		{
			if (aliases != null)
				foreach (var item in aliases)
					this.types.Add(item.Key, item.Value);
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>(1024);

		// Don't use static for this--it's not thread safe because contract resolver is stateful
		private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
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