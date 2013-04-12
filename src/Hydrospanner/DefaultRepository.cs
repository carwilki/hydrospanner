namespace Hydrospanner
{
	using System.Collections.Generic;
	using Wireup;

	public class DefaultRepository : IRepository
	{
		public IEnumerable<object> GetMementos()
		{
			yield return this.graveyard.GetMemento();

			foreach (var hydratable in this.catalog.Values)
				yield return hydratable.GetMemento();
		}

		public IEnumerable<IHydratable> Load(object message, Dictionary<string, string> headers)
		{
			this.loaded.Clear();

			foreach (var info in this.routes.Lookup(message, headers))
			{
				if (this.graveyard.Contains(info.Key))
					continue;

				this.loaded.Add(this.catalog.ValueOrDefault(info.Key) ?? (this.catalog[info.Key] = info.Create()));
			}

			return this.loaded;
		}

		public void Delete(IHydratable hydratable)
		{
			this.graveyard.Bury(hydratable.Key);
			this.catalog.Remove(hydratable.Key);
		}

		public void Restore(object memento)
		{
			if (!this.graveyardRestored && this.RestoreGraveyard(memento as GraveyardMemento))
				return;

			var hydratable = this.routes.Restore(memento);
			if (hydratable != null)
				this.catalog[hydratable.Key] = hydratable;
		}
		private bool RestoreGraveyard(GraveyardMemento memento)
		{
			if (memento == null)
				return false;

			var keys = memento.Keys;
			for (var i = 0; i < keys.Length; i++)
				this.graveyard.Bury(keys[i]);

			this.graveyardRestored = true;
			return true;
		}

		public DefaultRepository(IRoutingTable routes)
		{
			this.routes = routes;
		}

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>();
		private readonly List<IHydratable> loaded = new List<IHydratable>();
		private readonly HydratableGraveyard graveyard = new HydratableGraveyard();
		private readonly IRoutingTable routes;
		private bool graveyardRestored;
	}
}