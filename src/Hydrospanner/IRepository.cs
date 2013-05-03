namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IRepository
	{
		IEnumerable<object> GetMementos();
		IEnumerable<KeyValuePair<string, object>> GetPublicMementos();

		IEnumerable<IHydratable<T>> Load<T>(Delivery<T> delivery);

		void Delete(IHydratable hydratable);
		void Restore(object memento);
	}
}