namespace Hydrospanner.Serialization
{
	public interface ISerializer
	{
		/// <summary>
		/// Gets the value which indicates the encoding mechanism used (gzip, bzip2, lzma, aes, etc.)
		/// </summary>
		string ContentEncoding { get; }

		/// <summary>
		/// Gets the MIME-type suffix (json, xml, binary, etc.)
		/// </summary>
		string ContentFormat { get; }

		byte[] Serialize(object graph);

		object Deserialize(byte[] serialized, string typeName);

		T Deserialize<T>(byte[] serialized);
	}
}