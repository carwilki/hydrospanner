namespace TestHarness
{
    using System;
    using System.Collections;
    using Accounting.Events;
    using Hydrospanner;

    public class TestStreamIdentifier : IStreamIdentifier<AccountClosedEvent>
    {
        public Guid DiscoverStreams(AccountClosedEvent message, Hashtable headers)
        {
            return message.AccountId;
        }
    }
}