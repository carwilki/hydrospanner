namespace Hydrospanner.Phases.Bootstrap
{
	internal class BootstrapJournalMessage
	{
		public long Sequence { get; set; }
		public string TypeName { get; set; }
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}