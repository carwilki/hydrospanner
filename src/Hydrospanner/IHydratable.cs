namespace Hydrospanner
{
	using System.Collections;
	using System.Collections.Generic;

	public interface IHydratable
	{
		void Hydrate(object message, Hashtable headers);
		IEnumerable<object> GatherMessages();
	}

	public interface IHydratable<T> : IHydratable
	{
		void Hydrate(T message, Hashtable headers);
	}
}