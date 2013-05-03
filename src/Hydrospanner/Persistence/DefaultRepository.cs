namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Wireup;

	public sealed class DefaultRepository : IRepository
	{
		public ICollection<IHydratable> Items
		{
			get { return this.catalog.Values; }
		}

		public IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery)
		{
			foreach (var info in this.routes.Lookup(delivery))
			{
				if (string.IsNullOrEmpty(info.Key))
					continue;

				if (this.graveyard.Contains(info.Key))
					continue;

				var hydratable = this.Load<T>(info);
				if (hydratable != null)
					yield return hydratable;
			}
		}

		private IHydratable<T> Load<T>(HydrationInfo info)
		{
			IHydratable hydratable;
			if (this.catalog.TryGetValue(info.Key, out hydratable))
				return hydratable as IHydratable<T>;

			hydratable = info.Create();
			if (hydratable == null)
				return null;

			this.catalog[info.Key] = hydratable;
			return hydratable as IHydratable<T>;
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