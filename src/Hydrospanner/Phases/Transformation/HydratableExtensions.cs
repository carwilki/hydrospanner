namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using HydrateCallback = System.Action<IHydratable, object, System.Collections.Generic.Dictionary<string, string>, bool>;

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

		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private static void HydrateDelegate<T>(IHydratable hydratable, object message, Dictionary<string, string> headers, bool live)
		{
			((IHydratable<T>)hydratable).Hydrate((T)message, headers, live);
		}
		// ReSharper restore SuspiciousTypeConversion.Global
		// ReSharper restore UnusedMember.Local

		private static readonly MethodInfo DelegateMethod = typeof(HydratableExtensions).GetMethod("HydrateDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly Dictionary<Type, HydrateCallback> MethodCache = new Dictionary<Type, HydrateCallback>();
	}
}