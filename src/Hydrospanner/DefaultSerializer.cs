namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using fastJSON;

	public sealed class DefaultSerializer
	{
		public object Deserialize(byte[] serialized, string typeName)
		{
			if (serialized == null || serialized.Length == 0)
				return null;

			var raw = DefaultEncoding.GetString(serialized);
			return FastSerializer.ToObject(raw, this.ResolveType(typeName));
		}
		public T Deserialize<T>(byte[] serialized)
		{
			if (serialized == null || serialized.Length == 0)
				return default(T);

			var raw = DefaultEncoding.GetString(serialized);
			return (T)FastSerializer.ToObject(raw, typeof(T));
		}
		public byte[] Serialize(object graph)
		{
			if (graph == null)
				return null;

			var serialized = FastSerializer.ToJSON(graph, DefaultParameters);
			return DefaultEncoding.GetBytes(serialized);
		}

		private Type ResolveType(string typeName)
		{
			// TODO: standardize/trim type names?

			Type registered;
			if (!this.types.TryGetValue(typeName, out registered))
				this.types[typeName] = registered = Type.GetType(typeName);

			return registered;
		}

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private static readonly JSON FastSerializer = JSON.Instance;
		private static readonly JSONParameters DefaultParameters = new JSONParameters
		{
			IgnoreCaseOnDeserialize = true,
			SerializeNullValues = false,
			UseFastGuid = true,
		};
		private readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
	}
}