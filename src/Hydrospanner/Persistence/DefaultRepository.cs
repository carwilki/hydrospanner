namespace Hydrospanner.Persistence
{
	using System.Collections;
	using System.Collections.Generic;
	using Wireup;

	public class DefaultRepository : IRepository
	{
		public IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery)
		{
			foreach (var info in this.routes.Lookup(delivery))
			{
				if (string.IsNullOrEmpty(info.Key))
					continue;

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
			var hydratable = this.routes.Restore(memento);
			if (hydratable != null)
				this.catalog[hydratable.Key] = hydratable;
		}

		public int Count
		{
			get { return this.catalog.Count; }
		}
		public IEnumerator<IHydratable> GetEnumerator()
		{
			return this.catalog.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public DefaultRepository(IRoutingTable routes) : this(routes, null)
		{
		}
		public DefaultRepository(IRoutingTable routes, HydratableGraveyard graveyard)
		{
			graveyard = graveyard ?? new HydratableGraveyard();
			this.catalog[graveyard.Key] = this.graveyard = graveyard;

			this.routes = routes;
		}

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>();
		private readonly HydratableGraveyard graveyard;
		private readonly IRoutingTable routes;
	}
}