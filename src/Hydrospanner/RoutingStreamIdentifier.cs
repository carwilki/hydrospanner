namespace Hydrospanner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class RoutingStreamIdentifier : IStreamIdentifier
	{
		public Guid DiscoverStreams(object message, Hashtable headers)
		{
			if (message == null)
				return Guid.Empty;

			IStreamIdentifier registered;
			if (this.identifiers.TryGetValue(message.GetType(), out registered))
				return registered.DiscoverStreams(message, headers);

			return Guid.Empty;
		}

		public void Register<T>(IStreamIdentifier<T> identifier)
		{
			this.identifiers[typeof(T)] = new GenericIdentifier<T>(identifier);
		}

		private readonly Dictionary<Type, IStreamIdentifier> identifiers = new Dictionary<Type, IStreamIdentifier>();

		private class GenericIdentifier<T> : IStreamIdentifier
		{
			public Guid DiscoverStreams(object message, Hashtable headers)
			{
				return this.inner.DiscoverStreams((T)message, headers);
			}

			public GenericIdentifier(IStreamIdentifier<T> inner)
			{
				this.inner = inner;
			}

			private readonly IStreamIdentifier<T> inner;
		}
	}
}