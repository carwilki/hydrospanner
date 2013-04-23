namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratable
	{
		string Key { get; }
		bool IsComplete { get; }
		bool IsPublicSnapshot { get; }
		object Memento { get; }
		ICollection<object> PendingMessages { get; }
	}

	public interface IHydratable<T> : IHydratable
	{
		void Hydrate(Delivery<T> delivery);
	}
}