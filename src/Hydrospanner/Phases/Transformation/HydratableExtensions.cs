namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using HydrateCallback = System.Action<IHydratable, object, System.Collections.Generic.Dictionary<string, string>, bool>;
	using KeysCallback = System.Func<IHydratableSelector, object, System.Collections.Generic.Dictionary<string, string>, System.Collections.Generic.IEnumerable<IHydratableKey>>;
	using MementoCallback = System.Func<IHydratableSelector, object, IHydratable>;

	internal static class HydratableExtensions
	{
		public static void Hydrate(this IHydratable hydratable, object message, Dictionary<string, string> headers, bool live)
		{
			if (hydratable == null || message == null)
				return;

			var type = message.GetType();
			var callback = MethodCache.Add(type, () => MakeHydrateDelegate(type));
			callback(hydratable, message, headers, live);
		}
		private static HydrateCallback MakeHydrateDelegate(Type messageType)
		{
			var method = DelegateMethod.MakeGenericMethod(messageType);
			var callback = Delegate.CreateDelegate(typeof(HydrateCallback), method);
			return (HydrateCallback)callback;
		}

		public static IEnumerable<IHydratableKey> Keys(this IHydratableSelector selector, object message, Dictionary<string, string> headers = null)
		{
			if (selector == null || message == null)
				return null;

			var type = message.GetType();
			var callback = KeysMethodCache.Add(type, () => MakeKeysDelegate(type));
			return callback(selector, message, headers);
		}
		private static KeysCallback MakeKeysDelegate(Type messageType)
		{
			var method = KeysDelegateMethod.MakeGenericMethod(messageType);
			var callback = Delegate.CreateDelegate(typeof(KeysCallback), method);
			return (KeysCallback)callback;
		}

		public static IHydratable Create(this IHydratableSelector selector, object memento)
		{
			if (selector == null)
				return null;

			var type = memento.GetType();
			var callback = MementoMethodCache.Add(type, () => MakeMementoDelegate(type));
			return callback(selector, memento);
		}
		private static MementoCallback MakeMementoDelegate(Type mementoType)
		{
			var method = MementoDelegateMethod.MakeGenericMethod(mementoType);
			var callback = Delegate.CreateDelegate(typeof(MementoCallback), method);
			return (MementoCallback)callback;
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private static void HydrateDelegate<T>(IHydratable hydratable, object message, Dictionary<string, string> headers, bool live)
		{
			((IHydratable<T>)hydratable).Hydrate((T)message, headers, live);
		}
		private static IEnumerable<IHydratableKey> KeysDelegate<T>(IHydratableSelector selector, object message, Dictionary<string, string> headers)
		{
			return ((IHydratableSelector<T>)selector).Keys((T)message, headers);
		}
		private static IHydratable MementoDelegate<T>(IHydratableSelector selector, object memento)
		{
			return ((IHydratableFactory<T>)selector).Create((T)memento);
		}
		// ReSharper restore SuspiciousTypeConversion.Global
		// ReSharper restore UnusedMember.Local

		private static readonly MethodInfo DelegateMethod = typeof(HydratableExtensions).GetMethod("HydrateDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly MethodInfo KeysDelegateMethod = typeof(HydratableExtensions).GetMethod("KeysDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly MethodInfo MementoDelegateMethod = typeof(HydratableExtensions).GetMethod("MementoDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly Dictionary<Type, HydrateCallback> MethodCache = new Dictionary<Type, HydrateCallback>();
		private static readonly Dictionary<Type, KeysCallback> KeysMethodCache = new Dictionary<Type, KeysCallback>();
		private static readonly Dictionary<Type, MementoCallback> MementoMethodCache = new Dictionary<Type, MementoCallback>(); 
	}
}