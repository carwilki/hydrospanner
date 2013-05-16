namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using Serialization;

	public sealed class SnapshotItem
	{
		public bool IsPublicSnapshot { get; set; }
		public long CurrentSequence { get; set; }
		public int MementosRemaining { get; set; }

		public string Key { get; set; }
		public object Memento { get; set; }
		public string MementoType { get; set; }
		public byte[] Serialized { get; set; }

		public uint ComputedHash { get; set; }

		public void AsPublicSnapshot(string key, object memento, Type mementoType, long sequence)
		{
			this.Clear();
			this.IsPublicSnapshot = true;
			this.CurrentSequence = sequence;
			this.Key = key;
			this.Memento = Clone(memento);

			var type = this.Memento == null ? mementoType : this.Memento.GetType();
			this.MementoType = type.ResolvableTypeName();
		}
		public void AsPartOfSystemSnapshot(long sequence, int remaining, object memento, Type mementoType)
		{
			this.Clear();
			this.Memento = Clone(memento);
			this.CurrentSequence = sequence;
			this.MementosRemaining = remaining;

			var type = this.Memento == null ? mementoType : this.Memento.GetType();
			this.MementoType = type.ResolvableTypeName();
		}
		public void Serialize(ISerializer serializer)
		{
			if (this.Memento == null)
				return;

			this.Serialized = serializer.Serialize(this.Memento);
			this.ComputedHash = this.Serialized.ComputeHash();
		}
		public void Clear()
		{
			this.IsPublicSnapshot = false;
			this.Key = null;
			this.Memento = null;
			this.MementoType = null;
			this.Serialized = null;
			this.CurrentSequence = this.MementosRemaining = 0;
			this.ComputedHash = 0;
		}
		private static object Clone(object memento)
		{
			var cloneable = memento as ICloneable;
			return cloneable == null ? memento : cloneable.Clone();
		}
	}
}