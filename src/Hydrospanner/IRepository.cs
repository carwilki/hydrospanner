namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository
	{
		IEnumerable<IHydratable> Items { get; }

		IEnumerable<object> GetMementos();
		IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery);

		void Add(IHydratable hydratable);
		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}