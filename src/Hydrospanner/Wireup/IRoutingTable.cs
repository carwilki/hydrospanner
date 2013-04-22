namespace Hydrospanner.Wireup
{
	using System.Collections.Generic;

	public interface IRoutingTable
	{
		IEnumerable<HydrationInfo> Lookup<T>(Delivery<T> delivery);
		IHydratable Restore(object memento);
	}
}