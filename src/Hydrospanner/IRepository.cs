namespace Hydrospanner
{
	using System.Collections.Generic;

	// NOTE: this implementation of IRepo keeps track of tomb-stoned objects which is extremely convenient because when we 
	// restore the state of the system from snapshots, it can just look for a special memento that it understands and restore it...
	
	public interface IRepository
	{
		IEnumerable<object> GetMemento();
		IEnumerable<IHydratable> Load(object message, Dictionary<string, string> headers);
		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}