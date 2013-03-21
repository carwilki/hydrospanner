namespace Hydrospanner.Phases.Bootstrap
{
	public sealed class BootstrapItem
	{
		public string SerializedType { get; set; }
		public byte[] SerializedMemento { get; set; }
		public object Memento { get; set; }

		public void AsSnapshot(string serializedType, byte[] memento)
		{
			this.SerializedType = serializedType;
			this.SerializedMemento = memento;
			this.Memento = null;
		}
	}
}