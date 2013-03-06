namespace TestHarness
{
    using System;
    using System.Collections.Generic;
    using Accounting.Events;
    using Hydrospanner;

    public class TestStreamIdentifier : IStreamIdentifier
    {
        public Guid DiscoverStreams(object message, Dictionary<string, string> headers)
        {
			var closed = message as AccountClosedEvent;
			return closed.AccountId;
        }
    }
}