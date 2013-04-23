namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratable
	{
		string Key { get; }
		bool IsComplete { get; }
		bool IsPublicSnapshot { get; }
		ICollection<object> PendingMessages { get; } 
		object GetMemento();
	}

	public interface IHydratable<T> : IHydratable
	{
		void Hydrate(Delivery<T> delivery);
	}
}