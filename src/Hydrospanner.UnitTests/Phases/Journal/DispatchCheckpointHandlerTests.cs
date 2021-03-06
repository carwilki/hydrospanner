﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using Machine.Specifications;
	using NSubstitute;
	using Persistence;

	public class DispatchCheckpointHandlerTests
	{
		public class when_a_batch_of_message_is_handled
		{
			Because of = () =>
			{
				handler.OnNext(Build(40), 0, false);
				handler.OnNext(Build(41), 1, false);
				handler.OnNext(Build(42), 2, true);
			};

			It should_checkpoint_the_highest_possible_sequence = () =>
				store.Received(1).Save(42);

			It should_invoke_the_checkpoint_a_single_time = () =>
				store.Received(1).Save(Arg.Any<long>());
		}

		public class when_multiple_batches_are_handled
		{
			Because of = () =>
			{
				handler.OnNext(Build(39), 0, false);
				handler.OnNext(Build(40), 1, true);

				handler.OnNext(Build(41), 2, false);
				handler.OnNext(Build(42), 3, true);
			};

			It should_checkpoint_at_each_end_of_batch = () =>
			{
				store.Received(1).Save(40);
				store.Received(1).Save(42);
			};

			It should_invoke_the_checkpoint_a_for_each_batch = () =>
				store.Received(2).Save(Arg.Any<long>());
		}

		public class when_a_single_message_is_handled_with_more_to_come_in_the_batch
		{
			Because of = () =>
				handler.OnNext(Build(40), 0, false);

			It should_NOT_invoke_the_checkpoint_behavior = () =>
				store.Received(0).Save(Arg.Any<long>());
		}

		public class when_the_incoming_message_sequence_is_less_than_the_persisted_value
		{
			Establish context = () =>
			{
				handler.OnNext(Build(40), 0, false);
				handler.OnNext(Build(41), 1, false);
				handler.OnNext(Build(42), 2, true);
			};

			// this could happen when we indicate that some messages should not be journaled or dispatched, e.g.
			// a given command arrives from the wire and we only want to invoke the behavior once, but thereafter
			// we don't want to see it
			Because of = () =>
				handler.OnNext(Build(0), 3, true);

			It should_checkpoint_the_highest_possible_sequence_exactly_once = () =>
				store.Received(1).Save(42);

			It should_invoke_the_checkpoint_a_single_time_only_with_the_value_expected = () =>
				store.Received(1).Save(Arg.Any<long>());
		}

		Establish context = () =>
		{
			store = Substitute.For<IDispatchCheckpointStore>();
			handler = new DispatchCheckpointHandler(store);
		};

		static JournalItem Build(long sequence)
		{
			return new JournalItem
			{
				MessageSequence = sequence,
			};
		}

		static DispatchCheckpointHandler handler;
		static IDispatchCheckpointStore store;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
