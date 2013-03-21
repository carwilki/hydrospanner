namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository
	{
		IEnumerable<IHydratable> Load(object message, Dictionary<string, string> headers);
		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}