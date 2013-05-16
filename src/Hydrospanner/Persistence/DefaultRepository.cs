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
		public IDictionary<IHydratable, long> Accessed
		{
			get { return this.accessed; }
		} 

		public IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery)
		{
			this.live = delivery.Live;

			foreach (var info in this.routes.Lookup(delivery))
			{
				if (string.IsNullOrEmpty(info.Key))
					continue;

				if (this.graveyard.Contains(info.Key))
					continue;

				var hydratable = this.Load<T>(info);
				if (hydratable == null)
					continue;

				this.Touch(hydratable, delivery.Sequence);
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
		private void Touch(IHydratable hydratable, long sequence)
		{
			if (this.live || !hydratable.IsPublicSnapshot)
				return;

			// the incoming sequence hasn't yet affected the results, but any callers
			// to that access collection will be invoking it *after* the message has
			// taken effect
			this.accessed[hydratable] = sequence;
		}

		public void Delete(IHydratable hydratable)
		{
			if (!this.live)
				this.accessed.Remove(hydratable);

			this.graveyard.Bury(hydratable.Key);
			this.catalog.Remove(hydratable.Key);
		}
		public void Restore(object memento)
		{
			var hydratable = this.RestoreMemento(memento);
			if (hydratable != null)
				this.catalog[hydratable.Key] = hydratable;
		}
		private IHydratable RestoreMemento(object memento)
		{
			var graveyardMemento = memento as GraveyardMemento;
			if (graveyardMemento != null)
				return HydratableGraveyard.Restore(graveyardMemento, this.graveyard);

			return this.routes.Restore(memento);
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

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>(1024 * 64);
		private readonly Dictionary<IHydratable, long> accessed = new Dictionary<IHydratable, long>(1024 * 64);
		private readonly HydratableGraveyard graveyard;
		private readonly IRoutingTable routes;
		private bool live;
	}
}