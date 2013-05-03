namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;

	public sealed class HydratableGraveyard : IHydratable
	{
		public string Key { get; private set; }
		public bool IsComplete
		{
			get { return false; }
		}
		public bool IsPublicSnapshot
		{
			get { return false; }
		}
		public object Memento
		{
			get { return this.GetMemento(); }
		}
		public ICollection<object> PendingMessages { get; private set; }

		public void Bury(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			while (this.window.Count >= this.capacity)
				this.graveyard.Remove(this.window.Dequeue());

			this.graveyard.Add(key);
			this.window.Enqueue(key);
		}
		public bool Contains(string key)
		{
			return !string.IsNullOrEmpty(key) && this.graveyard.Contains(key);
		}
		public GraveyardMemento GetMemento()
		{
			return new GraveyardMemento(this.window.ToArray());
		}

		public HydratableGraveyard(GraveyardMemento graveyard = null, int capacity = 1024 * 1024)
		{
			this.Key = "/internal/graveyard";
			this.PendingMessages = new object[0];
			this.capacity = capacity;
			var keys = graveyard == null ? new string[0] : graveyard.Keys;
			this.graveyard = new HashSet<string>(keys);
			this.window = new Queue<string>(this.graveyard);
		}

		public static HydratableGraveyard Restore(GraveyardMemento memento)
		{
			return new HydratableGraveyard(memento);
		}

		private readonly int capacity;
		private readonly HashSet<string> graveyard;
		private readonly Queue<string> window;
	}
}