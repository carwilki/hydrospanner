namespace Hydrospanner.Phases.Bootstrap
{
	using System;

	public sealed class JournaledMessage
	{
		public long Sequence { get; set; }
		public string SerializedType { get; set; }
		public Guid ForeignId { get; set; }
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}