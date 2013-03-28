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
			foreach (var key in this.routes.Lookup(message, headers))
			{
				if (this.graveyard.Contains(key))
					continue;

				yield return this.catalog.ValueOrDefault(key) ?? (this.catalog[key] = this.routes.Create(message, headers));
			}
		}

		public void Delete(IHydratable hydratable)
		{
			this.graveyard.Bury(hydratable.Key);
			this.catalog.Remove(hydratable.Key);
		}

		public void Restore(object memento)
		{
			if (memento is string[])
				this.graveyard = new HydratableGraveyard(memento);
			else
			{
				var hydratable = this.routes.Create(memento);
				this.catalog[hydratable.Key] = hydratable;
			}
		}

		public DefaultRepository(IRoutingTable routes)
		{
			this.routes = routes;
		}

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>();
		private readonly IRoutingTable routes;
		private HydratableGraveyard graveyard = new HydratableGraveyard();
	}
}