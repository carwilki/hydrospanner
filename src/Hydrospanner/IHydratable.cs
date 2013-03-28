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
}