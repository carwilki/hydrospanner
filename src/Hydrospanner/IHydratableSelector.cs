namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IHydratableSelector
	{
		string[] Keys(object message, Dictionary<string, string> headers = null);
		IHydratable Create(string key, object message);
	}

	public interface IHydratableSelector<T>
	{
		string[] Keys(T message, Dictionary<string, string> headers = null);
		IHydratable Create(string key, T message);
	}
}