namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
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
				var serialized = JsonConvert.SerializeObject(graph, this.settings);
				return DefaultEncoding.GetBytes(serialized);
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
				var json = DefaultEncoding.GetString(serialized);
				return JsonConvert.DeserializeObject<T>(json, this.settings);
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
				return JsonConvert.DeserializeObject(DefaultEncoding.GetString(serialized), type, this.settings);
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

		public JsonSerializer(IDictionary<string, Type> types = null)
		{
			if (types != null)
				foreach (var item in types)
					this.types.Add(item.Key, item.Value);
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>(1024);

		// Don't use static for this--it's not thread safe because contract resolver is stateful
		private readonly JsonSerializerSettings settings = new JsonSerializerSettings
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