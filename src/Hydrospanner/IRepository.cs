namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository
	{
		IEnumerable<object> GetMementos();
		IEnumerable<IHydratable> Load<T>(Delivery<T> delivery);
		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}