namespace TestHarness
{
    using System;
    using System.Collections.Generic;
    using Hydrospanner;

    public class TestStreamIdentifier : IStreamIdentifier
    {
        public Guid DiscoverStreams(object message, Dictionary<string, string> headers)
        {
	        return Guid.Empty;
        }
    }
}