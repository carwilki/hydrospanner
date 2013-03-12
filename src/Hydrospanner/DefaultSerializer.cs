namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public sealed class DefaultSerializer
	{
		public object Deserialize(byte[] serialized, string typeName)
		{
			using (var stream = new MemoryStream(serialized))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (var jsonReader = new JsonTextReader(streamReader))
				return this.serializer.Deserialize(jsonReader, this.ResolveType(typeName));
		}
		public T Deserialize<T>(byte[] serialized)
		{
			using (var stream = new MemoryStream(serialized))
			using (var streamReader = new StreamReader(stream, DefaultEncoding))
			using (var jsonReader = new JsonTextReader(streamReader))
				return this.serializer.Deserialize<T>(jsonReader);
		}
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return new byte[0];

			using (var stream = new MemoryStream())
			using (var streamWriter = new StreamWriter(stream, DefaultEncoding))
			{
				using (var jsonWriter = new JsonTextWriter(streamWriter))
					this.serializer.Serialize(jsonWriter, graph);

				return stream.ToArray();
			}
		}

		private Type ResolveType(string typeName)
		{
			// TODO: standardize/trim type names?

			Type registered;
			if (!this.types.TryGetValue(typeName, out registered))
				this.types[typeName] = registered = Type.GetType(typeName);

			return registered;
		}

		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private readonly JsonSerializer serializer = new JsonSerializer
		{
#if DEBUG
			Formatting = Formatting.Indented,
#endif
			TypeNameHandling = TypeNameHandling.None,
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