namespace Hydrospanner
{
	using System.Collections.Generic;
	using Wireup;

	public class DefaultRepository : IRepository
	{
		public IEnumerable<object> GetMementos()
		{
			yield return this.graveyard; // TODO: sliding window, underlying collection

			foreach (var hydratable in this.catalog.Values)
				yield return hydratable.GetMemento();
		}

		public IEnumerable<IHydratable> Load(object message, Dictionary<string, string> headers)
		{
			foreach (var key in this.routes.Lookup(message, headers))
			{
				if (this.graveyard.Contains(key)) 
					continue;

				var hydratable = this.catalog.ValueOrDefault(key);
				yield return hydratable ?? this.routes.Create(message, headers);
			}
		}

		public void Delete(IHydratable hydratable)
		{
			
		}

		public void Restore(object memento)
		{
			// if memento is a graveyard memento, add to graveyard

			// otherwise:

			var hydratable = this.routes.Create(memento);
			this.catalog[hydratable.Key] = hydratable;
		}

		public DefaultRepository(IRoutingTable routes)
		{
			this.routes = routes;
		}

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>();
		private readonly HashSet<string> graveyard = new HashSet<string>();
		private readonly IRoutingTable routes;
	}
}