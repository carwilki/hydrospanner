namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;

	public class HydratableGraveyard
	{
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
			this.capacity = capacity;
			var keys = graveyard == null ? new string[0] : graveyard.Keys;
			this.graveyard = new HashSet<string>(keys);
			this.window = new Queue<string>(this.graveyard);
		}

		private readonly int capacity;
		private readonly HashSet<string> graveyard;
		private readonly Queue<string> window;
	}
}