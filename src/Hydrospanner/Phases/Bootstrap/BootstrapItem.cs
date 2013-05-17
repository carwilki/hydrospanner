namespace Hydrospanner.Phases.Bootstrap
{
	using System;

	public sealed class BootstrapItem
	{
		public string Key { get; set; }
		public string SerializedType { get; set; }
		public byte[] SerializedMemento { get; set; }
		public object Memento { get; set; }
		public Type MementoType { get; set; }

		public void AsSnapshot(string key, string serializedType, byte[] memento)
		{
			this.Key = key;
			this.SerializedType = serializedType;
			this.SerializedMemento = memento;
			this.Memento = null;
			this.MementoType = null;
		}
	}
}