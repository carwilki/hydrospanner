#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Messaging;
	using NSubstitute;

	public class DispatchHandlerTests
	{
		public class when_a_batch_containing_a_single_dispatch_item_is_handled
		{
			Establish context = () =>
				toDispatch = CreateDispatchItem();

			Because of = () =>
				handler.OnNext(toDispatch, 0, true);

			It should_send_the_item = () =>
				sender.Received(1).Send(toDispatch);

			It should_commit_the_transaction = () =>
				sender.Received(1).Commit();

			static JournalItem toDispatch;
		}

		public class when_a_batch_containing_multiple_dispatch_items_is_handled
		{
			Establish context = () =>
				items = new List<JournalItem>(new[] { CreateDispatchItem(), CreateDispatchItem(), CreateDispatchItem() });

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, false);
				handler.OnNext(items[2], 2, true);
			};

			It should_send_each_item = () =>
				items.ShouldBeLike(sent);

			It should_commit_the_transaction = () =>
				sender.Received(1).Commit();

			static List<JournalItem> items;
		}

		public class when_a_batch_containing_NO_dispatch_items_is_handled
		{
			Establish context = () =>
				items = new List<JournalItem>(new[] { CreateNonDispatchItem(), CreateNonDispatchItem(), CreateNonDispatchItem() });

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, false);
				handler.OnNext(items[2], 2, true);
			};

			It should_NOT_send_anything = () =>
				sender.Received(0).Send(Arg.Any<JournalItem>());

			It should_NOT_commit_the_transaction = () =>
				sender.Received(0).Commit();

			static List<JournalItem> items;
		}

		public class when_a_mixed_batch_of_some_dispatch_some_non_dispatch_items_is_handled
		{
			Establish context = () =>
				items = new List<JournalItem>(new[] { CreateNonDispatchItem(), CreateNonDispatchItem(), CreateDispatchItem() });

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, false);
				handler.OnNext(items[2], 2, true);
			};

			It should_only_send_the_dispatch_items = () =>
				sent.ShouldBeLike(new[] { items[2] });

			It should_commit_the_transaction = () =>
				sender.Received(1).Commit();

			static List<JournalItem> items;
		}

		public class when_the_last_item_in_a_mix_batch_is_a_non_dispatch_item
		{
			Establish context = () =>
				items = new List<JournalItem>(new[] { CreateDispatchItem(), CreateNonDispatchItem(), CreateNonDispatchItem() });

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, false);
				handler.OnNext(items[2], 2, true);
			};

			It should_only_send_the_dispatch_items = () =>
				sent.ShouldBeLike(items.Where(x => x.ItemActions == JournalItemAction.Dispatch));

			It should_commit_the_transaction = () =>
				sender.Received(1).Commit();

			static List<JournalItem> items;
		}

		public class when_a_batch_is_complete_and_another_batch_arrives
		{
			Establish context = () =>
			{
				first = new List<JournalItem>(new[] { CreateDispatchItem(), CreateNonDispatchItem(), CreateNonDispatchItem() });
				second = new List<JournalItem>(new[] { CreateDispatchItem(), CreateNonDispatchItem(), CreateNonDispatchItem() });

				handler.OnNext(first[0], 0, false);
				handler.OnNext(first[1], 1, false);
				handler.OnNext(first[2], 2, true);

				sent.Clear();
			};

			Because of = () =>
			{
				handler.OnNext(second[0], 3, false);
				handler.OnNext(second[1], 4, false);
				handler.OnNext(second[2], 5, true);
			};

			It should_only_send_the_dispatch_items_from_the_new_batch = () =>
				sent.ShouldBeLike(second.Where(x => x.ItemActions == JournalItemAction.Dispatch));

			It should_commit_each_transaction = () =>
				sender.Received(2).Commit();

			static List<JournalItem> first;
			static List<JournalItem> second;
		}

		public class when_committing_a_batch_fails
		{
			Establish context = () =>
			{
				items = new List<JournalItem>(new[] { CreateDispatchItem(), CreateDispatchItem() });
				sender.Commit().Returns(false, true);
			};

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, true);
			};

			It should_resend_the_same_batch_again = () =>
				sent.ShouldBeLike(items.Concat(items));

			It should_commit_the_transaction = () =>
				sender.Received(2).Commit();

			static List<JournalItem> items;
		}

		public class when_sending_a_message_fails
		{
			Establish context = () =>
			{
				items = new List<JournalItem>(new[] { CreateDispatchItem(), CreateDispatchItem() });
				sender.Send(Arg.Any<JournalItem>()).Returns(true, false, true, true);
			};

			Because of = () =>
			{
				handler.OnNext(items[0], 0, false);
				handler.OnNext(items[1], 1, true);
			};

			It should_resend_the_same_batch_again = () =>
				sent.ShouldBeLike(items.Concat(items));

			It should_commit_the_transaction = () =>
				sender.Received(1).Commit();

			static List<JournalItem> items;
		}

		static JournalItem CreateDispatchItem()
		{
			return new JournalItem
			{
				MessageSequence = sequence++, 
				ItemActions = JournalItemAction.Dispatch
			};
		}
		static JournalItem CreateNonDispatchItem()
		{
			return new JournalItem { ItemActions = JournalItemAction.None };
		}

		Establish context = () =>
		{
			sent = new List<JournalItem>();
			sender = Substitute.For<IMessageSender>();
			sender.Commit().Returns(true);
			sender.Send(Arg.Do<JournalItem>(sent.Add)).Returns(x => true);
			handler = new DispatchHandler(sender);
		};

		static int sequence;
		static DispatchHandler handler;
		static IMessageSender sender;
		static List<JournalItem> sent;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
