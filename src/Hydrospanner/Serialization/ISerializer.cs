namespace Hydrospanner.Serialization
{
	public interface ISerializer
	{
		byte[] Serialize(object graph);

		T Deserialize<T>(byte[] serialized);
		object Deserialize(byte[] serialized, string typeName);
	}
}