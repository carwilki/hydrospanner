namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public interface IStreamIdentifier
	{
		Guid DiscoverStreams(object message, Dictionary<string, string> headers);
	}

	public interface IStreamIdentifier<T>
	{
		Guid DiscoverStreams(T message, Dictionary<string, string> headers);
	}
}