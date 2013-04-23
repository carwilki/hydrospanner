namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Wireup;

	public class DefaultRepository : IRepository
	{
		public IEnumerable<IHydratable> Items
		{
			get { return this.catalog.Values; }
		}

		public IEnumerable<object> GetMementos()
		{
			var graveyardMemento = this.graveyard.GetMemento();
			if (graveyardMemento.Keys.Length > 0)
				yield return graveyardMemento;

			foreach (var hydratable in this.catalog.Values)
			{
				var memento = hydratable.GetMemento();
				if (memento != null)
					yield return memento;
			}
		}

		public IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery)
		{
			foreach (var info in this.routes.Lookup(delivery))
			{
				if (string.IsNullOrEmpty(info.Key))
					continue; // TODO: get this under test

				if (this.graveyard.Contains(info.Key))
					continue;

				IHydratable hydratable;
				if (this.catalog.TryGetValue(info.Key, out hydratable))
					yield return hydratable as IHydratable<T>;
				else
				{
					hydratable = info.Create();
					if (hydratable == null)
						continue;

					this.catalog[info.Key] = hydratable;
					yield return hydratable as IHydratable<T>;
				}
			}
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
		private readonly HydratableGraveyard graveyard = new HydratableGraveyard();
		private readonly IRoutingTable routes;
		private bool graveyardRestored;
	}
}