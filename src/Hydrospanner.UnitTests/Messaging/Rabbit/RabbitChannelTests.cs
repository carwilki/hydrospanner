#pragma warning disable 169, 414, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using Machine.Specifications;
	using NSubstitute;
	using Phases.Journal;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Framing.v0_9_1;

	[Subject(typeof(RabbitChannel))]
	public class when_communicating_with_the_broker
	{
		public class when_a_null_connector_is_provided
		{
			Because of = () =>
				Try(() => new RabbitChannel(null, NodeId));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_a_zero_is_provided_as_the_node_id
		{
			Because of = () =>
				Try(() => new RabbitChannel(connector, 0));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentOutOfRangeException>();
		}

		public class when_a_negative_number_is_provided_as_the_node_id
		{
			Because of = () =>
				Try(() => new RabbitChannel(connector, -1));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentOutOfRangeException>();
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

		public class when_attempting_to_send_a_message_without_a_payload
		{
			Because of = () =>
				result = channel.Send(NoPayload);

			It should_not_pass_the_message_to_underlying_channel = () =>
				actualChannel.Received(0).BasicPublish(Arg.Any<string>(), string.Empty, Arg.Any<IBasicProperties>(), Arg.Any<byte[]>());

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();

			static readonly JournalItem NoPayload = new JournalItem
			{
				ItemActions = JournalItemAction.Dispatch,
			};
		}

		public class when_attempting_to_send_a_message_with_an_unknown_type
		{
			Because of = () =>
				result = channel.Send(NoMessageType);

			It should_not_pass_the_message_to_underlying_channel = () =>
				actualChannel.Received(0).BasicPublish(Arg.Any<string>(), string.Empty, Arg.Any<IBasicProperties>(), Arg.Any<byte[]>());

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();

			static readonly JournalItem NoMessageType = new JournalItem
			{
				ItemActions = JournalItemAction.Dispatch,
				SerializedBody = new byte[] { 1, 2, 3, 4 },
				SerializedType = null
			};
		}

		public class when_sending_a_message
		{
			Establish context = () =>
				messageToSend.Headers["test-header"] = "test-value";

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_attempt_to_establish_a_connection_to_the_broker = () =>
				connector.Received(1).OpenChannel();

			It should_indicate_the_message_type_in_the_metadata = () =>
				properties.Type.ShouldEqual(messageToSend.SerializedType);

			It should_indicate_the_correct_timestamp = () =>
				properties.Timestamp.ShouldEqual(new AmqpTimestamp(SystemTime.EpochUtcNow));

			It should_indicate_the_content_type = () =>
				properties.ContentType.ShouldEqual(ContentType);

			It should_indicate_the_application_id = () =>
				properties.AppId.ShouldEqual(NodeId.ToString(CultureInfo.InvariantCulture));

			It should_indicate_the_outgoing_message_id = () =>
				properties.MessageId.ShouldEqual(((messageToSend.MessageSequence << 16) + NodeId).ToString(CultureInfo.InvariantCulture));

			It should_populate_the_headers = () =>
			{
				properties.Headers.Count.ShouldEqual(messageToSend.Headers.Count);
				foreach (var item in messageToSend.Headers)
					properties.Headers[item.Key].ShouldEqual(item.Value);
			};

			It should_mark_the_message_as_persistent = () =>
				properties.DeliveryMode.ShouldEqual((byte)PersistMessage);

			It should_pass_the_message_to_the_underlying_channel = () =>
				actualChannel.Received(1).BasicPublish("some-type", string.Empty, properties, messageToSend.SerializedBody);

			It should_indicate_success_to_the_caller = () =>
				result.ShouldBeTrue();
		}

		public class when_sending_additional_messages
		{
			Establish context = () =>
			{
				channel.Send(messageToSend);
				messageToSend.Headers["test-header"] = "test-value";
			};

			Because of = () =>
				result = channel.Send(messageToSend);

			It should_NOT_attempt_to_establish_a_connection_to_the_broker = () =>
				connector.Received(1).OpenChannel();

			It should_indicate_the_message_type_in_the_metadata = () =>
				properties.Type.ShouldEqual(messageToSend.SerializedType);

			It should_indicate_the_correct_timestamp = () =>
				properties.Timestamp.ShouldEqual(new AmqpTimestamp(SystemTime.EpochUtcNow));

			It should_indicate_the_content_type = () =>
				properties.ContentType.ShouldEqual(ContentType);

			It should_indicate_the_application_id = () =>
				properties.AppId.ShouldEqual(NodeId.ToString(CultureInfo.InvariantCulture));

			It should_indicate_the_outgoing_message_id = () =>
				properties.MessageId.ShouldEqual(((messageToSend.MessageSequence << 16) + NodeId).ToString(CultureInfo.InvariantCulture));

			It should_populate_the_headers = () =>
			{
				properties.Headers.Count.ShouldEqual(messageToSend.Headers.Count);
				foreach (var item in messageToSend.Headers)
					properties.Headers[item.Key].ShouldEqual(item.Value);
			};

			It should_mark_the_message_as_persistent = () =>
				properties.DeliveryMode.ShouldEqual((byte)PersistMessage);

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
				result = channel.Commit();

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

		public class when_attempting_to_receive_a_message_for_the_first_time
		{
			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_open_a_channel = () =>
				connector.Received(1).OpenChannel();

			It should_open_a_new_subscription = () =>
				factoryInvocations.ShouldEqual(1);

			It should_provide_the_opened_channel_to_the_subscription = () =>
				receivedChannel.ShouldEqual(actualChannel);

			It should_set_the_incoming_buffer_size = () =>
				actualChannel.Received(1).BasicQos(0, ushort.MaxValue, false);

			It should_attempt_to_receive_from_the_underlying_subscription = () =>
				subscription.Received(1).Receive(Timeout);
		}

		public class when_establishing_a_channel_with_the_broker_fails
		{
			Establish context = () =>
				connector.OpenChannel().Returns((IModel)null);

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_return_an_empty_delivery = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_attempting_to_receive_a_message_against_a_failed_channel
		{
			Establish context = () =>
				actualChannel.IsOpen.Returns(false);

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_dispose_the_underlying_subscription = () =>
				subscription.Received(1).Dispose();

			It should_return_an_empty_delivery = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_establishing_a_new_subscription_fails
		{
			Establish context = () =>
				channel = new RabbitChannel(connector, NodeId, x => { throw new Exception(); });

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_not_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_return_an_empty_message = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_disposing_a_failed_receiving_channel_throws_an_exception
		{
			Establish context = () =>
			{
				actualChannel.IsOpen.Returns(false);
				actualChannel.When(x => x.Dispose()).Do(x => { throw new Exception(); });
			};

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_suppress_the_exception = () =>
				thrown.ShouldBeNull();

			It should_return_an_empty_delivery = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_disposing_a_failed_subscription_throws_an_exception
		{
			Establish context = () =>
			{
				actualChannel.IsOpen.Returns(false);
				subscription.When(x => x.Dispose()).Do(x => { throw new Exception(); });
			};

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_suppress_the_exception = () =>
				thrown.ShouldBeNull();

			It should_return_an_empty_delivery = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_receiving_additional_messages
		{
			Establish context = () =>
				delivery = channel.Receive(Timeout);

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_not_open_additional_channels = () =>
				connector.Received(1).OpenChannel();

			It should_only_set_the_incoming_buffer_size_once = () =>
				actualChannel.Received(1).BasicQos(0, ushort.MaxValue, false);

			It should_not_open_additional_subscriptions = () =>
				factoryInvocations.ShouldEqual(1);
		}

		public class when_setting_the_incoming_buffer_size_throws_an_exception
		{
			Establish context = () =>
			{
				actualChannel.When(x => x.BasicQos(0, ushort.MaxValue, false)).Do(x => { throw new Exception(); });
				actualChannel.When(x => x.Dispose()).Do(x => { throw new Exception(); });
			};

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_return_an_empty_message = () =>
				delivery.Populated.ShouldBeFalse();

			It should_SAFELY_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();
		}

		public class when_attempting_to_receive_messages_against_a_disposed_channel
		{
			Establish context = () =>
				channel.Dispose();

			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_return_an_empty_delivery = () =>
				delivery.ShouldEqual(MessageDelivery.Empty);
		}

		public class when_disposing_the_channel_after_starting_to_receive_messages
		{
			Establish context = () =>
			{
				actualChannel.When(x => x.Dispose()).Do(x => { throw new Exception(); });
				subscription.When(x => x.Dispose()).Do(x => { throw new Exception(); });
				delivery = channel.Receive(Timeout);
			};

			Because of = () =>
				channel.Dispose();

			It should_SAFELY_dispose_the_underlying_channel = () =>
				actualChannel.Received(1).Dispose();

			It should_SAFELY_dispose_the_underlying_subscription = () =>
				subscription.Received(1).Dispose();
		}

		public class when_the_subscription_yields_an_empty_rabbit_message
		{
			Because of = () =>
				delivery = channel.Receive(Timeout);

			It should_return_an_empty_delivery = () =>
				delivery.Populated.ShouldBeFalse();
		}

		public class when_a_delivery_is_received
		{
			Establish context = () =>
			{
				rabbitMessage = BuildMessage();
				subscription.Receive(Timeout).Returns(rabbitMessage);
			};

			Because of = () =>
				delivery = channel.Receive(Timeout);

			public class for_regular_deliveries
			{
				Establish context = () =>
				{
					rabbitMessage.BasicProperties.MessageId = Guid.NewGuid().ToString();
					rabbitMessage.BasicProperties.AppId = "some-id";
				};

				It should_convert_the_rabbit_message_to_a_delivery = () =>
				delivery.Populated.ShouldBeTrue();

				It should_populate_the_delivery_payload = () =>
					delivery.Payload.ShouldEqual(rabbitMessage.Body);

				It should_populate_the_message_type = () =>
					delivery.MessageType.ShouldEqual(rabbitMessage.BasicProperties.Type);

				It should_populate_the_message_id = () =>
					delivery.MessageId.ShouldEqual(Guid.Parse(rabbitMessage.BasicProperties.MessageId));

				It should_copy_all_headers_to_the_delivery = () =>
					delivery.Headers.Count.ShouldEqual(rabbitMessage.BasicProperties.Headers.Count);

				It should_convert_all_incoming_headers_to_strings = () =>
				{
					foreach (var key in rabbitMessage.BasicProperties.Headers.Keys)
						delivery.Headers[(string)key].ShouldEqual(Encoding.UTF8.GetString((byte[])rabbitMessage.BasicProperties.Headers[key]));
				};

				It should_provide_a_callback_acknowledgment_to_confirm_the_message_delivery = () =>
					delivery.Acknowledge.ShouldNotBeNull();
			}

			public class when_the_delivery_message_identifier_is_numeric
			{
				Establish context = () =>
					rabbitMessage.BasicProperties.MessageId = "1025";

				It should_parse_the_identifier_as_a_guid = () =>
					delivery.MessageId.ShouldEqual(new Guid(0, 0, 0, BitConverter.GetBytes(long.Parse(rabbitMessage.BasicProperties.MessageId))));
			}

			public class when_the_delivery_message_identifier_is_is_empty
			{
				Establish context = () =>
					rabbitMessage.BasicProperties.MessageId = null;

				It should_return_a_random_guid_so_we_can_identify_it_as_a_foreign_message_at_startup = () =>
					delivery.MessageId.ShouldNotEqual(Guid.Empty);
			}

			public class when_the_delivery_message_identifier_cannot_be_parsed
			{
				Establish context = () =>
					rabbitMessage.BasicProperties.MessageId = "can't parse this";

				It should_return_a_random_guid_so_we_can_identify_it_as_a_foreign_message_at_startup = () =>
					delivery.MessageId.ShouldNotEqual(Guid.Empty);
			}

			public class when_the_delivered_message_originates_from_this_node
			{
				Establish context = () =>
					rabbitMessage.BasicProperties.AppId = NodeId.ToString(CultureInfo.InvariantCulture);

				It should_return_an_empty_delivery_to_the_caller = () =>
					delivery.Populated.ShouldBeFalse();
			}

			public class when_invoking_the_delivery_acknowledgment_callback
			{
				It should_acknowledge_the_delivery_tag_to_the_underlying_channel = () =>
				{
					delivery.Acknowledge();
					actualChannel.Received(1).BasicAck(rabbitMessage.DeliveryTag, true);
				};
			}

			public class when_acknowledging_a_delivery_against_a_disposed_channel
			{
				It should_NOT_invoke_the_ack_against_the_underlying_channel = () =>
				{
					channel.Dispose();
					delivery.Acknowledge();
					actualChannel.Received(0).BasicAck(Arg.Any<ulong>(), Arg.Any<bool>());
				};
			}

			public class when_acknowledging_a_delivery_against_a_failed_channel
			{
				It should_not_invoke_the_ack_against_the_underlying_channel = () =>
				{
					actualChannel.IsOpen.Returns(false);
					delivery.Acknowledge();
					actualChannel.Received(0).BasicAck(Arg.Any<ulong>(), Arg.Any<bool>());
				};
			}

			public class when_invoking_the_delivery_acknowledgment_callback_throws_an_exception
			{
				It should_suppress_the_exception = () =>
				{
					actualChannel.When(x => x.BasicAck(Arg.Any<ulong>(), Arg.Any<bool>())).Do(x => { throw new Exception(); });
					delivery.Acknowledge();
				};
			}

			static BasicDeliverEventArgs rabbitMessage;
		}

		Establish context = () =>
		{
			SystemTime.Freeze(DateTime.UtcNow);

			connector = Substitute.For<RabbitConnector>();
			actualChannel = Substitute.For<IModel>();
			properties = new BasicProperties();
			actualChannel.CreateBasicProperties().Returns(properties);
			actualChannel.IsOpen.Returns(true);

			connector.OpenChannel().Returns(actualChannel);

			factoryInvocations = 0;
			subscription = Substitute.For<RabbitSubscription>();
			subscriptionFactory = channel =>
			{
				factoryInvocations++;
				receivedChannel = channel;
				return subscription;
			};

			when_communicating_with_the_broker.channel = new RabbitChannel(connector, NodeId, subscriptionFactory);
		};

		static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		static BasicDeliverEventArgs BuildMessage()
		{
			var headers = new Hashtable
			{
				{ "Key1", Encoding.UTF8.GetBytes("Value1") },
				{ "Key2", Encoding.UTF8.GetBytes("Value2") }
			};
			var meta = new BasicProperties
			{
				Headers = headers,
				Type = "SomeNamespace.SomeClass",
				MessageId = Guid.NewGuid().ToString()
			};
			return new BasicDeliverEventArgs
			{
				BasicProperties = meta,
				DeliveryTag = 42,
				Body = new byte[] { 1, 2, 3, 4, 5 },
			};
		}

		Cleanup after = () =>
		{
			thrown = null;
			delivery = MessageDelivery.Empty;
			receivedChannel = null;
			SystemTime.Unfreeze();
		};

		static readonly JournalItem messageToSend = new JournalItem
		{
			ItemActions = JournalItemAction.Dispatch,
			MessageSequence = 1234,
			SerializedType = "Some.Type",
			SerializedBody = new byte[] { 0, 1, 2, 3, 4 },
			Headers = new Dictionary<string, string>()
		};

		const short PersistMessage = 2;
		const short NodeId = 42;
		const string ContentType = "application/vnd.nmb.hydrospanner-msg";
		static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1234);
		static bool result;
		static IModel actualChannel;
		static RabbitChannel channel;
		static RabbitConnector connector;
		static Exception thrown;
		static IBasicProperties properties;
		static RabbitSubscription subscription;
		static Func<IModel, RabbitSubscription> subscriptionFactory;
		static int factoryInvocations;
		static MessageDelivery delivery;
		static IModel receivedChannel;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414