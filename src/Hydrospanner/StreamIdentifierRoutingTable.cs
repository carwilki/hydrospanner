namespace Hydrospanner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class StreamIdentifierRoutingTable : IStreamIdentifier<object>
	{
		Guid IStreamIdentifier<object>.DiscoverStreams(object message, Hashtable headers)
		{
			if (message == null)
				return Guid.Empty;

			IStreamIdentifier<object> registered;
			if (this.identifiers.TryGetValue(message.GetType(), out registered))
				return registered.DiscoverStreams(message, headers);

			return Guid.Empty;
		}

		public void Register<T>(IStreamIdentifier<T> identifier)
		{
			this.identifiers[typeof(T)] = new GenericIdentifier<T>(identifier);
		}

		private readonly Dictionary<Type, IStreamIdentifier<object>> identifiers = new Dictionary<Type, IStreamIdentifier<object>>();

		private class GenericIdentifier<T> : IStreamIdentifier<object>
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