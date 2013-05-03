namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	public sealed class ReflectionDeliveryHandler : IDeliveryHandler
	{
		public IEnumerable<object> Deliver(TransformationItem item, bool live)
		{
			var callback = this.CreateCallback(item.Body.GetType());
			return callback(item.Body, item.Headers, item.MessageSequence, live, item.ForeignId == Guid.Empty);
		}
		public IEnumerable<object> Deliver(object message, long sequence)
		{
			var callback = this.CreateCallback(message.GetType());
			return callback(message, EmptyHeaders, sequence, true, true);
		}
		private HandleDelegate CreateCallback(Type messageType)
		{
			HandleDelegate callback;
			if (!this.callbacks.TryGetValue(messageType, out callback))
				this.callbacks[messageType] = callback = this.CreateCallback(this.callbackDelegateMethod.MakeGenericMethod(messageType));

			return callback;
		}
		private HandleDelegate CreateCallback(MethodInfo method)
		{
			return (HandleDelegate)Delegate.CreateDelegate(typeof(HandleDelegate), this, method);
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private IEnumerable<object> RegisterCallbackDelegate<T>(object message, Dictionary<string, string> headers, long sequence, bool live, bool local)
		{
			var delivery = new Delivery<T>((T)message, headers, sequence, live, local);
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

		private static readonly Dictionary<string, string> EmptyHeaders = new Dictionary<string, string>(); 
		private readonly Dictionary<Type, HandleDelegate> callbacks = new Dictionary<Type, HandleDelegate>();
		private readonly MethodInfo callbackDelegateMethod;
		private readonly ITransformer handler;
		private delegate IEnumerable<object> HandleDelegate(object message, Dictionary<string, string> headers, long sequence, bool live, bool local);
	}
}