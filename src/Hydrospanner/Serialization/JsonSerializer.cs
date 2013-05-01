namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;
	using Newtonsoft.Json.Serialization;

	internal class JsonSerializer : ISerializer
	{
		public string ContentEncoding
		{
			get { return "utf8"; }
		}
		public string ContentFormat
		{
			get { return "json"; }
		}

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

			var type = this.LoadType(typeName);
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
		private Type LoadType(string typeName)
		{
			if (string.IsNullOrWhiteSpace(typeName))
				return null;

			Type type;
			if (!this.types.TryGetValue(typeName, out type))
				this.types[typeName] = type = Type.GetType(typeName);

			return type;
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
			Converters = { new UnderscoreEnumConverter(), new StringEnumConverter() },
			ContractResolver = new UnderscoreContractResolver()
		};
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>(1024);
	}

	internal class UnderscoreContractResolver : DefaultContractResolver
	{
		public override JsonContract ResolveContract(Type type)
		{
			if (type.HasJsonUnderscoreAttribute())
				return base.ResolveContract(type);

			return this.resolver.ResolveContract(type);
		}
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);
			property.PropertyName = member.ParseContractName() ?? this.normalizer.Normalize(property.PropertyName);
			return property;
		}

		private readonly DefaultContractResolver resolver = new DefaultContractResolver();
		private readonly UnderscoreNormalizer normalizer = new UnderscoreNormalizer();
	}
	internal class UnderscoreEnumConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
		{
			var parsed = this.normalizer.Normalize(((Enum)value).ToString("G"));
			writer.WriteValue(parsed);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.String)
				return this.converter.ReadJson(reader, objectType, existingValue, serializer);

			var raw = reader.Value.ToString();
			if (!string.IsNullOrEmpty(raw))
				raw = raw.Replace("_", string.Empty);

			return Enum.Parse(objectType, raw, true);
		}
		public override bool CanConvert(Type objectType)
		{
			return this.converter.CanConvert(objectType) && objectType.HasJsonUnderscoreAttribute();
		}

		private readonly StringEnumConverter converter = new StringEnumConverter();
		private readonly UnderscoreNormalizer normalizer = new UnderscoreNormalizer();
	}

	internal static class UnderscoreExtensions
	{
		public static bool HasJsonUnderscoreAttribute(this Type type)
		{
			if (type == null)
				return false;

			var descriptions = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);
			for (var i = 0; i < descriptions.Length; i++)
				if (descriptions[i].Description == "json:underscore")
					return true;

			return false;
		}
		public static string ParseContractName(this MemberInfo member)
		{
			var attributes = (DataMemberAttribute[])member.GetCustomAttributes(typeof(DataMemberAttribute), false);
			return attributes.Length == 0 ? null : attributes[0].Name;
		}
	}
}