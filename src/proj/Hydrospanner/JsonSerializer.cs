namespace Hydrospanner
{
	using System;
	using System.Runtime.Serialization;
	using System.Text;
	using fastJSON;

	internal class JsonSerializer
	{
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return null;
			
			try
			{
				var serialized = Serializer.ToJSON(graph);
				return DefaultEncoding.GetBytes(serialized);
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}

		public object Deserialize<T>(byte[] serialize)
		{
			var json = DefaultEncoding.GetString(serialize);
			
			return Serializer.ToObject<T>(json);
		}

		static JsonSerializer()
		{
			Serializer.Parameters.UseUTCDateTime = false;
			Serializer.Parameters.EnabledNameNameVariantFlags = NameVariants.WithUnderscoresLowercase;
		}

		private static readonly JSON Serializer = JSON.Instance;
		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
	}
}