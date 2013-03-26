namespace Hydrospanner.Wireup
{
	public sealed class JournaledMessage
	{
		public long Sequence { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}