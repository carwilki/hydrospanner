namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratable
	{
		void Hydrate(object message, Dictionary<string, string> headers, bool replay);
		IEnumerable<object> GatherMessages();
	}

	public interface IHydratable<T>
	{
		void Hydrate(T message, Dictionary<string, string> headers, bool replay);
		IEnumerable<object> GatherMessages();
	}
}