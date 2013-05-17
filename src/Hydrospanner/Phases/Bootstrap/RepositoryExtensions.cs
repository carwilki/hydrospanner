namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	internal static class RepositoryExtensions
	{
		public static void Restore(this IRepository repository, string key, Type mementoType, object memento)
		{
			var callback = LoadCallback(mementoType);
			callback(repository, key, memento);
		}
		private static RestoreDelegate LoadCallback(Type type)
		{
			RestoreDelegate callback;
			if (!Callbacks.TryGetValue(type, out callback))
				Callbacks[type] = callback = CreateCallback(CallbackDelegateMethod.MakeGenericMethod(type));

			return callback;
		}
		private static RestoreDelegate CreateCallback(MethodInfo method)
		{
			return (RestoreDelegate)Delegate.CreateDelegate(typeof(RestoreDelegate), null, method);
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private static void RegisterCallbackDelegate<T>(IRepository repository, string key, object memento)
		{
			memento = memento ?? default(T);
			repository.Restore(key, (T)memento);
		}
		// ReSharper restore SuspiciousTypeConversion.Global
		// ReSharper restore UnusedMember.Local

		private delegate void RestoreDelegate(IRepository repository, string key, object memento);
		private static readonly MethodInfo CallbackDelegateMethod = typeof(RepositoryExtensions).GetMethod("RegisterCallbackDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly Dictionary<Type, RestoreDelegate> Callbacks = new Dictionary<Type, RestoreDelegate>();
	}
}