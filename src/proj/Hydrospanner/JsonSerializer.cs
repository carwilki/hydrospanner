namespace Hydrospanner
{
	using System;
	using System.Runtime.Serialization;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	internal class JsonSerializer
	{
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return null;
			
			try
			{
				var serialized = JsonConvert.SerializeObject(graph, Settings);
				return DefaultEncoding.GetBytes(serialized);
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

			var type = LoadType(typeName);
			if (type == null)
				throw new SerializationException("Type '{0}' not found.".FormatWith(typeName));

			try
			{
				return JsonConvert.DeserializeObject(DefaultEncoding.GetString(serialized), type, Settings);
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}
		private static Type LoadType(string typeName)
		{
			if (string.IsNullOrWhiteSpace(typeName))
				return null;

			return Type.GetType(typeName);
		}
		public T Deserialize<T>(byte[] serialized)
		{
			if (serialized == null || serialized.Length == 0)
				return default(T);

			try
			{
				var json = DefaultEncoding.GetString(serialized);
				return JsonConvert.DeserializeObject<T>(json, Settings);
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			TypeNameHandling = TypeNameHandling.None,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			Converters = { new StringEnumConverter() },
#if DEBUG
			Formatting = Formatting.Indented
#endif
		};
	}
}