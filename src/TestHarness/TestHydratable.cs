namespace TestHarness
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Accounting.Events;
    using Hydrospanner;

    public class TestHydratable : IHydratable
    {
        public void Hydrate(object message, Hashtable headers)
        {
            // provide to underlying aggregate/saga/projector
        }

        public IEnumerable<object> GatherMessages()
        {
            return new object[]
                {
                    new AccountClosedEvent
                        {
                            AccountId = Guid.NewGuid(),
                            Description = "Hello, World!",
                            Dispatched = DateTime.UtcNow,
                            MessageId = Guid.NewGuid(),
                            Reason = CloseReason.Abuse,
                            UserId = Guid.NewGuid(),
                            Username = "test@test.com"
                        }
                };
        }
    }
}