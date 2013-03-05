namespace Hydrospanner
{
	using System;
	using System.Collections;

	public interface IStreamIdentifier<T>
	{
		Guid DiscoverStreams(T message, Hashtable headers);
	}
}