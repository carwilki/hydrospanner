namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratable
	{
		string Key { get; }
		bool IsComplete { get; }
		bool IsPublicSnapshot { get; }
		IEnumerable<object> GatherMessages();
		object GetMemento();
	}

	public interface IHydratable<T>
	{
		void Hydrate(T message, Dictionary<string, string> headers, bool live);
	}

	public interface IHydratableKey
	{
		string Name { get; }
		IHydratable Create();
	}

	public interface IHydratableSelector
	{
	}

	public interface IHydratableSelector<T> : IHydratableSelector
	{
		IEnumerable<IHydratableKey> Keys(T message, Dictionary<string, string> headers = null);
	}

	public interface IHydratableFactory<T> : IHydratableSelector
	{
		IHydratable Create(T memento);
	}
}