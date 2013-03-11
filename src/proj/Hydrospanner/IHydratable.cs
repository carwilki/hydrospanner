namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratable
	{
		string Key { get; }
		bool IsComplete { get; }
		IEnumerable<object> GatherMessages();
		object GetMemento();

		void LoadFromMemento(object memento);
		bool PublicSnapshot { get; }
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