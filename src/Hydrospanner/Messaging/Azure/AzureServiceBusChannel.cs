using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydrospanner.Messaging.Azure
{
    using Hydrospanner.Phases.Journal;
    using Hydrospanner.Phases.Snapshot;

    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusChannel : IMessageSender, IMessageReceiver
    {
        private readonly string _queue;

        private MessagingFactory _factory;

        private QueueClient _queueClient;

        public AzureServiceBusChannel(string queue)
        {
            _queue = queue;
            _factory = MessagingFactory.Create();
            _queueClient = _factory.CreateQueueClient(_queue);
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            
        }

        public MessageDelivery Receive(TimeSpan timeout)
        {
            try
            {
                var ret = _queueClient.Receive(timeout);
                var message = ret.GetBody<MessageDelivery>();
                ret.Complete();
                return message;
            }
            catch (TimeoutException)
            {
                return MessageDelivery.Empty;
            }
        }

        #endregion

        #region Implementation of IMessageSender

        public bool Send(SnapshotItem message)
        {
            return false;
        }

        public bool Send(JournalItem message)
        {
            return false;
        }

        public bool Commit()
        {
            return false;
        }

        #endregion
    }
}
