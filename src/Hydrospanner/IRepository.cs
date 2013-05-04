namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository
	{
		ICollection<IHydratable> Items { get; }
		IDictionary<IHydratable, long> Accessed { get; }

		IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery);

		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}