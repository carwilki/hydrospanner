#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;
	using Machine.Specifications;
	using NSubstitute;
	using RabbitMQ.Client;

	[Subject(typeof(RabbitChannel))]
	public class when_communicating_with_the_broker
	{
		public class when_a_null_connector_is_provided
		{
			Because of = () =>
				Try(() => new RabbitChannel(null));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_disposing_the_channel
		{
			Because of = () =>
				channel.Dispose();

			It should_dispose_the_underlying_connector = () =>
				connector.Received(1).Dispose();
		}

		public class when_disposing_the_channel_throws_an_exception
		{
			Establish context = () =>
				connector.When(x => x.Dispose()).Do(x => { throw new Exception(); });

			Because of = () =>
				channel.Dispose();

			It should_dispose_the_underlying_connector = () =>
				connector.Received(1).Dispose();
		}

		public class when_disposing_the_channel_multiple_times
		{
			Establish context = () =>
				channel.Dispose();

			Because of = () =>
				channel.Dispose();

			It should_dispose_the_underlying_connector_exactly_once = () =>
				connector.Received(1).Dispose();
		}

		public class when_sending_a_null_message
		{
			Because of = () =>
				Try(() => channel.Send(null));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_sending_a_message_that_should_NOT_be_dispatched
		{
			Because of = () =>
				result = channel.Send(new JournalItem());

			It should_not_pass_the_message_to_underlying_channel = () =>
				actualChannel.Received(0).BasicPublish(Arg.Any<string>(), string.Empty, Arg.Any<IBasicProperties>(), Arg.Any<byte[]>());

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_sending_a_message
		{
			Because of = () =>
				result = channel.Send(messageToSend);

			It should_attempt_to_establish_a_connection_to_the_broker = () =>
				connector.Received(1).OpenChannel();

			It should_pass_the_message_to_the_underlying_channel = () =>
				actualChannel.Received(1).BasicPublish("some-type", string.Empty, properties, messageToSend.SerializedBody);

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_sending_additional_messages
		{
			Establish context = () =>
				channel.Send(messageToSend);

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_NOT_attempt_to_establish_a_connection_to_the_broker = () =>
				connector.Received(1).OpenChannel();

			It should_pass_the_message_to_the_underlying_channel = () =>
				actualChannel.Received(2).BasicPublish("some-type", string.Empty, properties, messageToSend.SerializedBody);

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_establishing_a_connection_with_the_broker_fails
		{
			Establish context = () =>
				connector.OpenChannel().Returns((IModel)null);

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_NOT_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();
		}

		public class when_sending_against_a_disposed_channel
		{
			Establish context = () =>
				channel.Dispose();

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();

			It should_NOT_throw_any_exceptions = () =>
				thrown.ShouldBeNull();
		}

		public class when_sending_a_message_throws_an_exception
		{
			Establish context = () =>
			{
				actualChannel
					.When(x => x.BasicPublish(Arg.Any<string>(), string.Empty, Arg.Any<IBasicProperties>(), Arg.Any<byte[]>()))
					.Do(x => { throw new Exception(); });

				actualChannel
					.When(x => x.Dispose())
					.Do(x => { throw new Exception(); });
			};

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_SAFELY_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_NOT_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();
		}

		public class when_sending_a_message_after_a_previously_failed_attempt
		{
			Establish context = () =>
			{
				connector.OpenChannel().Returns(null, actualChannel);
				channel.Send(messageToSend);
			};

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_attempt_to_reconnect_to_the_broker = () =>
				connector.Received(2).OpenChannel();

			It should_pass_the_message_to_the_underlying_channel = () =>
				actualChannel.Received(1).BasicPublish("some-type", string.Empty, properties, messageToSend.SerializedBody);

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_disposing_after_sending_a_message
		{
			Establish context = () =>
			{
				connector.When(x => x.Dispose()).Do(x => { throw new Exception(); });
				actualChannel.When(x => x.Dispose()).Do(x => { throw new Exception(); });

				channel.Send(messageToSend);
			};

			Because of = () =>
				channel.Dispose();

			It should_SAFELY_dipose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_SAFELY_dipose_the_underlying_connector = () =>
				connector.Received(1).Dispose();

			It should_NOT_raise_any_exceptions = () =>
				thrown.ShouldBeNull();
		}

		public class when_the_broker_reconnection_fails
		{
			Establish context = () =>
			{
				connector.OpenChannel().Returns(x => (count++ >= 1) ? null : actualChannel);

				actualChannel
					.When(x => x.BasicPublish(Arg.Any<string>(), string.Empty, Arg.Any<IBasicProperties>(), Arg.Any<byte[]>()))
					.Do(x => { throw new Exception(); });

				channel.Send(messageToSend); // fails and shuts down connection
			};

			Because of = () =>
				result = channel.Send(messageToSend); // connection is null, return false

			It should_attempt_to_reconnect_to_the_broker = () =>
				connector.Received(2).OpenChannel();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();

			It should_NOT_raise_any_exceptions = () =>
				thrown.ShouldBeNull();

			static int count;
		}

		public class when_committing_a_transaction_without_first_sending
		{
			Because of = () =>
				result = channel.Commit();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();

			It should_NOT_commit_against_the_underlying_channel = () =>
				actualChannel.Received(0).TxCommit();
		}

		public class when_committing_a_transaction
		{
			Establish context = () =>
				channel.Send(messageToSend);

			Because of = () =>
				channel.Commit();

			It should_commit_against_the_underlying_channel = () =>
				actualChannel.Received(1).TxCommit();

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_committing_a_transaction_throws
		{
			Establish context = () =>
			{
				actualChannel.When(x => x.TxCommit()).Do(x => { throw new Exception(); });
				channel.Send(messageToSend);
			};

			Because of = () =>
				result = channel.Commit();

			It should_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_NOT_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();
		}

		public class when_committing_against_a_disposed_channel
		{
			Establish context = () =>
				channel.Dispose();

			Because of = () =>
				result = channel.Commit();

			It should_indicate_failure_to_the_caller = () =>
				result.ShouldBeFalse();

			It should_NOT_throw_any_exceptions = () =>
				thrown.ShouldBeNull();
		}

		public class when_receiving_a_message
		{
		}

		Establish context = () =>
		{
			connector = Substitute.For<RabbitConnector>();
			actualChannel = Substitute.For<IModel>();
			connector.OpenChannel().Returns(actualChannel);
			channel = new RabbitChannel(connector);
			actualChannel.CreateBasicProperties().Returns(properties);
		};

		static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		Cleanup after = () =>
			thrown = null;

		static readonly JournalItem messageToSend = new JournalItem
		{
			ItemActions = JournalItemAction.Dispatch,
			MessageSequence = 1234,
			SerializedType = "Some.Type",
			SerializedBody = new byte[] { 0, 1, 2, 3, 4 },
			Headers = new Dictionary<string, string>()
		};
		static bool result;
		static IModel actualChannel;
		static RabbitChannel channel;
		static RabbitConnector connector;
		static Exception thrown;
		static IBasicProperties properties;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169