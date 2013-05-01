namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
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
			Converters = { new StringEnumConverter() },
			ContractResolver = new UnderscoreContractResolver()
		};
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>(1024);
	}

	internal class UnderscoreContractResolver : DefaultContractResolver
	{
		protected override string ResolvePropertyName(string propertyName)
		{
			return this.normalizer.Normalize(propertyName);
		}

		private readonly UnderscoreNormalizer normalizer = new UnderscoreNormalizer();
	}

	internal class UnderscoreNormalizer
	{
		public string Normalize(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			this.builder.Clear();

			var len = value.Length;
			var canUnderscore = false;
			for (var i = 0; i < len; i++)
			{
				var letter = value[i];
				var upper = char.IsUpper(letter);

				canUnderscore = canUnderscore || !upper;

				if (upper)
				{
					if (i > 0 && canUnderscore)
						this.builder.Append("_");
					else if (i > 0 && i + 1 < len && !char.IsUpper(value[i + 1]))
						this.builder.Append("_");

					letter = char.ToLower(letter);
				}

				this.builder.Append(letter);
			}

			return this.builder.ToString();
		}

		private readonly StringBuilder builder = new StringBuilder(1024);
	}
}