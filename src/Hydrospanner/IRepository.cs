namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository : IEnumerable<IHydratable>
	{
		IEnumerable<object> GetMementos();
		IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery);

		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}