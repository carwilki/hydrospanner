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
}