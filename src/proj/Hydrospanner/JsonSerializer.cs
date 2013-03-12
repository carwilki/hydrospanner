namespace Hydrospanner
{
	using System;
	using System.Runtime.Serialization;
	using System.Text;
	using Newtonsoft.Json;

	internal class JsonSerializer
	{
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return null;
			
			try
			{
				var serialized = JsonConvert.SerializeObject(graph);
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
			return JsonConvert.DeserializeObject<T>(json);
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
	}
}