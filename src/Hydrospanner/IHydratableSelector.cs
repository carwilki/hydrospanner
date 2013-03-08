namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratableSelector
	{
		IEnumerable<IHydratableKey> Keys(object message, Dictionary<string, string> headers = null);
	}

	public interface IHydratableSelector<T>
	{
		IEnumerable<IHydratableKey> Keys(T message, Dictionary<string, string> headers = null);
	}
}