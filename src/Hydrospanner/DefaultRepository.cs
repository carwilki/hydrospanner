namespace Hydrospanner
{
	using System.Collections.Generic;
	using System.Linq;
	using Phases.Transformation;

	public class DefaultRepository : IRepository
	{
		public IEnumerable<object> GetMementos()
		{
			return this.catalog.Select(item => item.Value.GetMemento());
		}

		public IEnumerable<IHydratable> Load(object message, Dictionary<string, string> headers)
		{
			return this.selector.Keys(message, headers)
				.Select(key => this.graveyard.ValueOrDefault(key.Name) 
					?? this.catalog.ValueOrDefault(key.Name) 
						?? key.Create());
		}

		public void Delete(IHydratable hydratable)
		{
			this.catalog.Remove(hydratable.Key);
			this.graveyard[hydratable.Key] = hydratable;
		}

		public void Restore(object memento)
		{
			// TODO: no idea here...

			// NOTE: this implementation of IRepo keeps track of tomb-stoned objects which is extremely convenient because when we 
			// restore the state of the system from snapshots, it can just look for a special memento that it understands and restore it...
		}

		public DefaultRepository(IHydratableSelector selector)
		{
			this.selector = selector;
		}

		private readonly Dictionary<string, IHydratable> catalog = new Dictionary<string, IHydratable>(); 
		private readonly Dictionary<string, IHydratable> graveyard = new Dictionary<string, IHydratable>();
		private readonly IHydratableSelector selector;
	}
}