namespace Hydrospanner
{
	using System;
	using System.Collections;

	public interface IStreamIdentifier
	{
		Guid DiscoverStreams(object message, Hashtable headers);
	}

	public interface IStreamIdentifier<T> : IStreamIdentifier
	{
		Guid DiscoverStreams(T message, Hashtable headers);
	}
}