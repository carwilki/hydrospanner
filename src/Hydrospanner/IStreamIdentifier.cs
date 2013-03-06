namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public interface IStreamIdentifier<T>
	{
		Guid DiscoverStreams(T message, Dictionary<string, string> headers);
	}
}