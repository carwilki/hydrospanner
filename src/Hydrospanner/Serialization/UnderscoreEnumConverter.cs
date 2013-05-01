namespace Hydrospanner.Serialization
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

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
}