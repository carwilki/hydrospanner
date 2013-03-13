#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Hydrospanner.Messaging;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(MessageListener))]
	public class when_listening_for_a_foreign_message_from_the_wire
	{
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
				listener.Start();

			It should_attempt_to_receive_from_the_underlying_messaging_handler = () =>
				receiver.Received(1).Receive(Arg.Any<TimeSpan>());

			It should_timeout_after_two_seconds = () =>
				receiver.Received().Receive(TimeSpan.FromSeconds(2));
		}

		public class when_the_listener_is_started_more_than_once
		{
			Establish context = () =>
				listener.Start();

			Because of = () =>
				listener.Start();

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
				Thread.Sleep(10);
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
				Thread.Sleep(10);
			};

			It should_push_the_message_to_the_ring_buffer = () =>
				harness.AllItems.Count.ShouldBeGreaterThan(0);

			It should_attempt_to_receive_another_message_from_the_underlying_messaging_handler_until_disposed = () =>
				receiver.Received(MaxReceives).Receive(Arg.Any<TimeSpan>());

			const int MaxReceives = 2;
			static readonly MessageDelivery Delivery = new MessageDelivery(
				Guid.NewGuid(), new byte[] { 0, 1, 2, 3 }, "some-type", new Dictionary<string, string>(), null);
			static int counter;
		}

		public class when_the_listener_is_disposed
		{
			Because of = () =>
				listener.Dispose();

			It should_dispose_the_underlying_messaging_handle = () =>
				receiver.Received(1).Dispose();
		}

		public class when_the_listener_is_disposed_more_than_once
		{
			Establish context = () =>
				listener.Dispose();

			Because of = () =>
				listener.Dispose();

			It should_dispose_the_underlying_messaging_handle = () =>
				receiver.Received(1).Dispose();
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
			listener = new MessageListener(receiver, harness.RingBuffer);
		};

		Cleanup after = () =>
		{
			thrown = null;
			listener.Dispose();
			harness.Dispose();
		};

		static IMessageReceiver receiver;
		static RingBufferHarness<TransformationItem> harness;
		static MessageListener listener;
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169