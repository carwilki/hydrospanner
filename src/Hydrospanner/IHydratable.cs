namespace Hydrospanner
{
	using System.Collections;

	public interface IHydratable
	{
		void Hydrate(object message, Hashtable headers);
	}
}