namespace Hydrospanner
{
	using System;
	using System.Collections;

	public interface IStreamIdentifier
	{
		Guid DiscoverStreams(object message, Hashtable headers);
	}
}