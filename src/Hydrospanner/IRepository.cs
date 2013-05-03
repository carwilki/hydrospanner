namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository : IEnumerable<IHydratable>
	{
		int Count { get; }
		IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery);

		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}