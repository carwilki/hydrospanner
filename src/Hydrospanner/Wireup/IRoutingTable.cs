namespace Hydrospanner.Wireup
{
	using System.Collections.Generic;

	public interface IRoutingTable
	{
		IEnumerable<string> Lookup(object message, Dictionary<string, string> headers);
		IHydratable Create(object message, Dictionary<string, string> headers);
		IHydratable Create(object memento);
	}
}