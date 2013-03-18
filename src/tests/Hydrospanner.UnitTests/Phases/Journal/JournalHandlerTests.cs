#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Hydrospanner.Persistence;
	using Machine.Specifications;
	using NSubstitute;

	public class when_journaling_messages
	{
		public class when_journaling_a_batch
		{
			Establish context = () =>
				handled.AddRange(new[] { CreateItem(), CreateItem(), CreateItem(), CreateItem() });

			It should_provide_the_set_of_messages_to_the_underlying_storage = () =>
				journaled.ShouldBeLike(handled);
		}

		public class when_journaling_has_not_received_a_complete_batch
		{
			Establish context = () =>
			{
				handled.AddRange(new[] { CreateItem(), CreateItem(), CreateItem(), CreateItem() });
				endOfBatchIndices = new[] { 4 }; // never submits end of batch
			};

			It should_NOT_provide_anything_to_the_underlying_storage = () =>
				journaled.ShouldBeEmpty();
		}

		public class when_there_are_no_messages_requesting_journal_behavior
		{
			Establish context = () =>
				handled.AddRange(new[] { CreateItem(false), CreateItem(false), CreateItem(false), CreateItem(false) });

			It should_NOT_provide_anything_to_the_underlying_storage = () =>
				store.Received(0).Save(Arg.Any<List<JournalItem>>());
		}

		public class when_only_some_messages_in_the_batch_request_journaling
		{
			Establish context = () =>
				handled.AddRange(new[] { CreateItem(), CreateItem(false), CreateItem(), CreateItem(false) });

			It should_only_provide_the_items_requesting_journaling_to_the_underlying_storage = () =>
				journaled.ShouldBeLike(handled.Where(x => x.ItemActions.HasFlag(JournalItemAction.Journal)));
		}

		public class when_journaling_fails
		{
			Establish context = () =>
			{
				store.Save(Arg.Any<List<JournalItem>>()).Returns(false, false, false, true);
				handled.AddRange(new[] { CreateItem(), CreateItem(), CreateItem(), CreateItem() });
			};

			It should_pause_the_current_thread_for_a_few_seconds_after_each_failure = () =>
			{
				threadSleepInvocations.ShouldEqual(3);
				threadSleep.ShouldEqual(TimeSpan.FromSeconds(5));
			};

			It should_retry_the_save_operation_with_the_same_list_until_success_is_received = () =>
				journaled.ShouldBeLike(handled.Concat(handled.Concat(handled).Concat(handled)));
		}

		public class when_another_batch_arrives_after_the_first_has_been_journaled
		{
			Establish context = () =>
			{
				handled.AddRange(new[] { CreateItem(), CreateItem(false), CreateItem(), CreateItem(false) });
				endOfBatchIndices = new[] { 1, 3 }; // two batches of two
			};

			It should_NOT_repeat_items_from_the_first_batch = () =>
				journaled.ShouldBeLike(handled.Where(x => x.ItemActions.HasFlag(JournalItemAction.Journal)));
		}

		Establish context = () =>
		{
			currentSequence = 0;
			endOfBatchIndices = null;
			journaled = new List<JournalItem>();
			handled = new List<JournalItem>();
			store = Substitute.For<IMessageStorage>();
			store.Save(Arg.Do<List<JournalItem>>(x => x.ForEach(journaled.Add))).Returns(true);
			handler = new JournalHandler(store);
			ThreadExtensions.Freeze(x =>
			{
				threadSleepInvocations++;
				threadSleep = x;
			});
		};

		Cleanup after = () =>
		{
			ThreadExtensions.Unfreeze();
			threadSleepInvocations = 0;
		};

		Because of = () =>
		{
			if (endOfBatchIndices == null)
				endOfBatchIndices = new[] { handled.Count - 1 };

			for (var i = 0; i < handled.Count; i++)
				handler.OnNext(handled[i], i + 1, endOfBatchIndices.Contains(i));
		};

		static JournalItem CreateItem(bool journal = true)
		{
			return new JournalItem
			{
				MessageSequence = ++currentSequence,
				ItemActions = journal ? JournalItemAction.Journal : JournalItemAction.None
			};
		}

		static JournalHandler handler;
		static List<JournalItem> handled;
		static List<JournalItem> journaled;
		static IMessageStorage store;
		static long currentSequence;
		static int[] endOfBatchIndices;
		static TimeSpan threadSleep;
		static int threadSleepInvocations;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
