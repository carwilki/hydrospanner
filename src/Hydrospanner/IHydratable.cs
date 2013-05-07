namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public interface IHydratable
	{
		string Key { get; }
		bool IsComplete { get; }
		object Memento { get; }
		ICollection<object> PendingMessages { get; }
	}

	public interface IPublicHydratable : IHydratable
	{
		Type MementoType { get; }
	}

	public interface IHydratable<T> : IHydratable
	{
		void Hydrate(Delivery<T> delivery);
	}
}