namespace Hydrospanner.Serialization
{
	using System;

	public interface ISerializer
	{
		byte[] Serialize(object graph);

		T Deserialize<T>(byte[] serialized);
		object Deserialize(byte[] serialized, string typeName);
		object Deserialize(byte[] serialized, string typeName, out Type deserializedType);
	}
}