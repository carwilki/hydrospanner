namespace Hydrospanner.Phases.Bootstrap
{
	using System;

	public sealed class BootstrapItem
	{
		public Type MementoType { get; set; }
		public byte[] SerializedMemento { get; set; }
		public object Memento { get; set; }

		public void AsSnapshot(Type type, byte[] memento)
		{
			this.MementoType = type;
			this.SerializedMemento = memento;
			this.Memento = null;
		}
	}
}