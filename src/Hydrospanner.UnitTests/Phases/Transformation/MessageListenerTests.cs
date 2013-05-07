#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using Messaging;
	using NSubstitute;

	[Subject(typeof(MessageListener))]
	public class when_listening_for_a_foreign_message_from_the_wire
	{
		public class when_no_receiver_is_provided
		{
			Because of = () =>
				Try(() => new MessageListener(null, harness, new DuplicateStore(1024), transients));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_no_ring_buffer_is_provided
		{
			Because of = () =>
				Try(() => new MessageListener(() => receiver, null, duplicates, transients));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_no_duplicate_store_is_provided
		{
			Because of = () =>
				Try(() => new MessageListener(() => receiver, harness, null, transients));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_no_transient_types_collection_is_provided
		{
			Because of = () =>
				Try(() => new MessageListener(() => receiver, harness, duplicates, null));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_the_listener_has_not_started 
		{
			It should_NOT_attempt_to_receive_from_the_underlying_messaging_handler = () =>
				receiver.Received(0).Receive(Arg.Any<TimeSpan>());

			It should_not_push_any_messages_to_the_ring_buffer = () =>
				harness.AllItems.Count.ShouldEqual(0);
		}

		public class when_the_listener_is_started
		{
			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_attempt_to_receive_from_the_underlying_messaging_handler = () =>
				receiver.Received().Receive(Arg.Any<TimeSpan>());

			It should_timeout_after_two_seconds = () =>
				receiver.Received().Receive(TimeSpan.FromSeconds(2));
		}

		public class when_the_listener_is_started_more_than_once
		{
			Establish context = () =>
				listener.Start();

			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_attempt_to_receive_from_the_underlying_messaging_handler = () =>
				receiver.Received().Receive(Arg.Any<TimeSpan>());
		}

		public class when_an_empty_message_arrives
		{
			Establish context = () =>
				receiver.Receive(Arg.Do<TimeSpan>(x => UponReceive())).Returns(new MessageDelivery());

			static void UponReceive()
			{
				if (++counter >= MaxReceives)
					listener.Dispose();
			}

			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_not_push_the_empty_message_to_the_Ring_buffer = () =>
				harness.AllItems.Count.ShouldEqual(0);

			It should_attempt_to_receive_another_message_from_the_underlying_messaging_handler_until_disposed = () =>
				receiver.Received(MaxReceives).Receive(Arg.Any<TimeSpan>());

			const int MaxReceives = 42;
			static int counter;
		}

		public class when_a_populated_message_arrives
		{
			Establish context = () =>
				receiver.Receive(Arg.Do<TimeSpan>(x => UponReceive())).Returns(Delivery);

			static void UponReceive()
			{
				if (++counter >= MaxReceives)
					listener.Dispose();
			}

			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_push_the_message_to_the_ring_buffer = () =>
				harness.AllItems.Count.ShouldBeGreaterThan(0);

			It should_indicate_the_item_to_be_persistent = () =>
				harness.AllItems.ToList().ForEach(x => x.IsTransient.ShouldBeFalse());

			It should_attempt_to_receive_another_message_from_the_underlying_messaging_handler_until_disposed = () =>
				receiver.Received(MaxReceives).Receive(Arg.Any<TimeSpan>());

			const int MaxReceives = 2;
			static readonly MessageDelivery Delivery = new MessageDelivery(
				Guid.NewGuid(), new byte[] { 0, 1, 2, 3 }, "some-type", new Dictionary<string, string>(), null);
			static int counter;
		}

		public class when_a_transient_message_arrives
		{
			Establish context = () =>
			{
				transients.Add(Delivery.MessageType);
				receiver.Receive(Arg.Do<TimeSpan>(x => UponReceive())).Returns(Delivery);
			};

			static void UponReceive()
			{
				if (++counter >= MaxReceives)
					listener.Dispose();
			}

			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_push_the_message_to_the_ring_buffer = () =>
				harness.AllItems.Count.ShouldBeGreaterThan(0);

			It should_indicate_the_item_to_be_persistent = () =>
				harness.AllItems.ToList().ForEach(x => x.IsTransient.ShouldBeTrue());

			It should_attempt_to_receive_another_message_from_the_underlying_messaging_handler_until_disposed = () =>
				receiver.Received(MaxReceives).Receive(Arg.Any<TimeSpan>());

			const int MaxReceives = 2;
			static readonly MessageDelivery Delivery = new MessageDelivery(
				Guid.NewGuid(), new byte[] { 0, 1, 2, 3 }, "some-type", new Dictionary<string, string>(), null);
			static int counter;
		}

		public class when_a_duplicate_message_arrives
		{
			Establish context = () =>
				receiver.Receive(Arg.Do<TimeSpan>(x => UponReceive())).Returns(Delivery);

			Cleanup after = () =>
				ack = Acknowledgment.ConfirmBatch;

			static void UponReceive()
			{
				if (++counter >= MaxReceives)
					listener.Dispose();
			}

			Because of = () =>
			{
				listener.Start();
				Thread.Sleep(50);
			};

			It should_NOT_push_the_message_to_the_ring_buffer = () =>
				harness.AllItems.Count.ShouldEqual(1);

			It should_acknowledge_receipt_of_the_message_to_the_underlying_channel = () =>
				ack.ShouldEqual(Acknowledgment.ConfirmSingle);

			It should_attempt_to_receive_another_message_from_the_underlying_messaging_handler_until_disposed = () =>
				receiver.Received(MaxReceives).Receive(Arg.Any<TimeSpan>());

			const int MaxReceives = 2;
			static readonly MessageDelivery Delivery = new MessageDelivery(
				Guid.NewGuid(), new byte[] { 0, 1, 2, 3 }, "some-type", new Dictionary<string, string>(), x => ack = x);
			static int counter;
			static Acknowledgment ack;
		}

		public class when_the_listener_is_disposed_after_starting
		{
			Establish context = () =>
				listener.Start();

			Because of = () =>
			{
				listener.Dispose();
				Thread.Sleep(50);
			};

			It should_dispose_the_underlying_messaging_handle = () =>
				receiver.Received(1).Dispose();
		}

		public class when_the_listener_is_disposed_more_than_once_after_starting
		{
			Establish context = () =>
			{
				listener.Start();
				listener.Dispose();
			};

			Because of = () =>
				listener.Dispose();

			It should_dispose_the_underlying_messaging_handle_exactly_once = () =>
				receiver.Received(1).Dispose();
		}

		public class when_the_listener_is_disposed_WITHOUT_starting
		{
			Because of = () =>
				listener.Dispose();

			It should_NOT_dispose_the_underlying_messaging_handle = () =>
				receiver.Received(0).Dispose();
		}

		public class when_the_listener_is_started_after_being_disposed
		{
			Establish context = () =>
				listener.Dispose();

			Because of = () =>
				Try(listener.Start);

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ObjectDisposedException>();
		}

		private static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		Establish context = () =>
		{
			receiver = Substitute.For<IMessageReceiver>();
			harness = new RingBufferHarness<TransformationItem>();
			duplicates = new DuplicateStore(1024);
			transients = new HashSet<string>();
			listener = new MessageListener(() => receiver, harness, duplicates, transients);
		};

		Cleanup after = () =>
		{
			thrown = null;
			listener.Dispose();
		};

		static IMessageReceiver receiver;
		static RingBufferHarness<TransformationItem> harness;
		static MessageListener listener;
		static DuplicateStore duplicates;
		static HashSet<string> transients; 
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
