namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	// TODO: get this under test
	public class ReflectionDeliveryHandler : IDeliveryHandler
	{
		public IEnumerable<object> Deliver(object message, Dictionary<string, string> headers, long sequence, bool live)
		{
			var type = message.GetType();
			var callback = this.callbacks.Add(type, () => this.RegisterCallback(type));
			return callback(message, headers, sequence, live);
		}

		private HandleDelegate RegisterCallback(Type messageType)
		{
			var method = this.callbackDelegateMethod.MakeGenericMethod(messageType);
			var callback = Delegate.CreateDelegate(typeof(HandleDelegate), this, method);
			return (HandleDelegate)callback;
		}
		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private IEnumerable<object> RegisterCallbackDelegate<T>(object message, Dictionary<string, string> headers, long sequence, bool live)
		{
			var delivery = new Delivery<T>((T)message, headers, sequence, live);
			return this.handler.Transform(delivery);
		}
		// ReSharper restore SuspiciousTypeConversion.Global
		// ReSharper restore UnusedMember.Local

		public ReflectionDeliveryHandler(ITransformer handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			this.handler = handler;
			this.callbackDelegateMethod = this.GetType().GetMethod("RegisterCallbackDelegate", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		private readonly Dictionary<Type, HandleDelegate> callbacks = new Dictionary<Type, HandleDelegate>();
		private readonly MethodInfo callbackDelegateMethod;
		private readonly ITransformer handler;
		internal delegate IEnumerable<object> HandleDelegate(object message, Dictionary<string, string> headers, long sequence, bool live);
	}
}