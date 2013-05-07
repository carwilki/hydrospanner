namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Globalization;
	using log4net;
	using Phases.Journal;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Exceptions;

	public class RabbitChannel : IMessageSender, IMessageReceiver
	{
		public bool Send(JournalItem message)
		{
			if (message == null)
				throw new ArgumentNullException();

			if (this.disposed)
				return false;

			if (!message.ItemActions.HasFlag(JournalItemAction.Dispatch))
				return true; // no op

			if (message.SerializedBody == null || message.SerializedBody.Length == 0)
				return true; // no op

			if (string.IsNullOrWhiteSpace(message.SerializedType))
				return true;

			var currentChannel = this.OpenChannel(false);
			if (currentChannel == null)
				return false;

			return this.Send(message, currentChannel);
		}
		private bool Send(JournalItem message, IModel currentChannel)
		{
			// FUTURE: Any correlation ID could potentially be stored in the message headers and then extracted.
			// Also, on the receiving side we could do the same thing in reverse.

			// FUTURE: TTL and DeliveryMode could be in an application-defined dictionary that is available for lookup here
			// based upon message type.  Default to Persistent, no TTL if an entry is not found.

			// FUTURE: ContentType and ContentEncoding will be dynamic based upon serialization, e.g. +json, +msgpack, +pb, etc.
			var meta = currentChannel.CreateBasicProperties();
			meta.AppId = this.normalizedNodeId;
			meta.DeliveryMode = Persistent;
			meta.Type = message.SerializedType;
			meta.Timestamp = new AmqpTimestamp(SystemTime.EpochUtcNow);
			meta.MessageId = message.MessageSequence.ToMessageId(this.nodeId);
			meta.ContentType = ContentType;
			meta.ContentEncoding = ContentEncoding;
			meta.Headers = message.Headers.CopyTo(meta.Headers);
			var exchange = message.SerializedType.NormalizeType();

			try
			{
				currentChannel.BasicPublish(exchange, string.Empty, meta, message.SerializedBody);
			}
			catch (AlreadyClosedException e)
			{
				var reason = e.ShutdownReason;
				if (reason != null && reason.Initiator == ShutdownInitiator.Peer && reason.ReplyCode == 404)
					Log.Fatal("Exchange '{0}' does not exist.".FormatWith(exchange), e); // CONFIG: use throttling to log4net xml config

				Wait.Sleep();
				this.Close();
				return false;
			}
			catch
			{
				this.Close();
				return false;
			}

			return true;
		}
		public bool Commit()
		{
			if (this.disposed)
				return false;

			var currentChannel = this.channel;
			if (currentChannel == null)
				return false;

			try
			{
				currentChannel.TxCommit();
				return true;
			}
			catch
			{
				this.Close();
				return false;
			}
		}

		public MessageDelivery Receive(TimeSpan timeout)
		{
			if (this.disposed)
				return MessageDelivery.Empty;

			var currentChannel = this.OpenChannel(true);
			if (currentChannel == null)
				return MessageDelivery.Empty;

			var currentSubscription = this.OpenSubscription(currentChannel);
			if (currentSubscription == null)
				return MessageDelivery.Empty;

			if (currentChannel.IsOpen)
				return this.ReceiveMessage(currentChannel, currentSubscription.Receive(timeout));

			this.Close();
			return MessageDelivery.Empty;
		}
		private MessageDelivery ReceiveMessage(IModel currentChannel, BasicDeliverEventArgs message)
		{
			if (message == null)
				return MessageDelivery.Empty;

			var meta = message.BasicProperties;
			if (meta.AppId == this.normalizedNodeId)
				return MessageDelivery.Empty; // the message originated at this node, don't re-consume it; FUTURE: how does this affect timeout messages?

			var id = meta.MessageId.ToMessageId();
			var headers = meta.Headers.Copy();
			var tag = message.DeliveryTag;

			return new MessageDelivery(id, message.Body, meta.Type, headers, ack =>
			{
				if (this.disposed || !currentChannel.IsOpen)
					return;

				try
				{
					AcknowledgeMessage(currentChannel, ack, tag);
				}
				catch
				{
// ReSharper disable RedundantJumpStatement
					return;
// ReSharper restore RedundantJumpStatement
				}
			});
		}
		private static void AcknowledgeMessage(IModel currentChannel, Acknowledgment acknowledgment, ulong tag)
		{
			// Threading notes:
			// According to the RabbitMQ Client PDF documentation for .NET, a single channel is not thread safe and access to it
			// must be serialized, but this typically manifest during RPC operations such as declaring a queue or
			// for *committing* a transaction or publishing a message large message.  For non-blocking operations (such as ack/reject)
			// it performs it's own synchronization.  In fact, a review of the client source code reveals a library absolutely
			// filled with lock() statements to perform it's own serialization of operations.  I ran several intensive tests to assert the
			// behavior below.  I created a console app that ran a single receiving and multiple publishing threads.  I was
			// able to run millions of messages through without any threading issues whatsoever.  It was only by introducing
			// channel.TxSelect() (along with channel.TxCommit) that I started to receive the "Pipelining of requests forbidden" message.
			// In this console app, the published messages on the sending threads were tiny and only occupied a single RabbitMQ TCP frame which
			// is almost certainly the reason for not having pipelining issues.  Similarly, the ack/reject instructions occupy
			// a single TCP frame and are "non-blocking" operations according to RabbitMQ's definition of blocking.  This means
			// that even though a transaction commit is only a single frame, it's a blocking operation which causes an exception to be thrown
			// because multiple threads are trying to commit the transaction and it's getting multiple send operations before receiving
			// confirmation of the committed transaction.  In other words, the code below is entirely thread safe SO LONG AS this channel
			// remains a non-transactional channel.  I also asserted that confirming multiple messages while individually rejecting individual
			// messages here and there worked as expected.
			if (Acknowledgment.ConfirmBatch == acknowledgment)
				currentChannel.BasicAck(tag, AcknowledgeMultiple);
			else if (Acknowledgment.RejectSingle == acknowledgment)
				currentChannel.BasicReject(tag, MarkAsDeadLetter);
			else
				currentChannel.BasicAck(tag, AcknowledgeSingle);
		}

		private IModel OpenChannel(bool receive)
		{
			var currentChannel = this.channel;
			if (currentChannel != null)
				return currentChannel;

			this.channel = currentChannel = this.connector.OpenChannel();
			if (currentChannel == null)
				return null;

			try
			{
				if (receive)
					currentChannel.BasicQos(0, ushort.MaxValue, false);
				else
					currentChannel.TxSelect();
			}
			catch
			{
				this.Close();
				return null;
			}

			return currentChannel;
		}
		private RabbitSubscription OpenSubscription(IModel currentChannel)
		{
			var currentSubscription = this.subscription;
			if (currentSubscription != null)
				return currentSubscription;

			try
			{
				currentSubscription = this.factory(currentChannel);
				this.subscription = currentSubscription;
				return currentSubscription;
			}
			catch
			{
				this.Close();
				return null;
			}
		}
		private void Close()
		{
			this.subscription = this.subscription.TryDispose();
			this.channel = this.channel.TryDispose();
		}

		public RabbitChannel(RabbitConnector connector, short nodeId, Func<IModel, RabbitSubscription> factory = null)
		{
			if (connector == null)
				throw new ArgumentNullException();

			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			this.connector = connector;
			this.nodeId = nodeId;
			this.factory = factory;
			this.normalizedNodeId = nodeId.ToString(CultureInfo.InvariantCulture);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;

			this.Close();
			this.connector.TryDispose();
		}

		private const byte Persistent = 2;
		private const string ContentType = "application/vnd.hydrospanner-msg+json";
		private const string ContentEncoding = "utf8";
		private const bool AcknowledgeSingle = false; // false = single ack
		private const bool AcknowledgeMultiple = true;
		private const bool MarkAsDeadLetter = false; // false = make it a dead letter
		private static readonly ILog Log = LogManager.GetLogger(typeof(RabbitChannel));
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(3);
		private readonly RabbitConnector connector;
		private readonly Func<IModel, RabbitSubscription> factory;
		private readonly short nodeId;
		private readonly string normalizedNodeId;
		private IModel channel;
		private RabbitSubscription subscription;
		private bool disposed;
	}
}