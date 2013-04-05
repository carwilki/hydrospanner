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
			foreach (var info in this.routes.Lookup(message, headers))
			{
				if (this.graveyard.Contains(info.Key))
					continue;

				yield return this.catalog.ValueOrDefault(info.Key) ?? (this.catalog[info.Key] = info.Create());
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
				var hydratable = this.routes.Restore(memento);
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