namespace Hydrospanner
{
	using System.Collections.Generic;

	public class HydratableGraveyard
	{
		public virtual void Bury(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			while (this.window.Count >= this.capacity)
				this.graveyard.Remove(this.window.Dequeue());

			this.graveyard.Add(key);
			this.window.Enqueue(key);
		}

		public virtual bool Contains(string key)
		{
			return !string.IsNullOrEmpty(key) && this.graveyard.Contains(key);
		}

		public virtual string[] GetMemento()
		{
			return this.window.ToArray();
		}

		public HydratableGraveyard(object graveyard = null, int capacity = 1024)
		{
			this.capacity = capacity;
			var input = graveyard as string[];
			this.graveyard = new HashSet<string>(input ?? new string[0]);
			this.window = new Queue<string>(this.graveyard);
		}

		private readonly int capacity;
		private readonly HashSet<string> graveyard;
		private readonly Queue<string> window;
	}
}