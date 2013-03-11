namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratableSelector
	{
		IEnumerable<IHydratableKey> Keys(object message, Dictionary<string, string> headers = null);
		IHydratable Create(object memento);
	}

	public interface IHydratableSelector<T>
	{
		IEnumerable<IHydratableKey> Keys(T message, Dictionary<string, string> headers = null);
	}

	public interface IHydratableFactory<T>
	{
		IHydratable Create(T memento);
	}
}