#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using Machine.Specifications;

	public class AcknowledgementHandlerTests
	{
		public class when_a_single_dispatch_item_is_ready_to_be_acknowledged
		{
			Because of = () =>
				handler.OnNext(Create(42), 0, true);

			It should_acknowledge_the_item = () =>
				acknowledged.ShouldEqual(42);

			It should_invoke_the_acknowledgement_callback_exactly_once = () =>
				invocations.ShouldEqual(1);
		}

		public class when_multiple_dispatch_items_are_ready_to_be_acknowledged
		{
			Establish context = () =>
				handler.OnNext(Create(41), 0, false);

			Because of = () =>
				handler.OnNext(Create(42), 1, true);

			It should_invoke_the_callback_for_the_highest_sequenced_item = () =>
				acknowledged.ShouldEqual(42);

			It should_invoke_the_acknowledgement_callback_exactly_once = () =>
				invocations.ShouldEqual(1);
		}

		public class when_the_last_item_in_the_set_is_not_a_dispatch_item
		{
			Establish context = () =>
			{
				handler.OnNext(Create(41), 0, false);
				handler.OnNext(Create(42), 1, false);
			};

			Because of = () =>
				handler.OnNext(Create(0), 2, true);

			It should_invoke_the_callback_for_the_highest_dispatch_item = () =>
				acknowledged.ShouldEqual(42);

			It should_invoke_the_acknowledgement_callback_exactly_once = () =>
				invocations.ShouldEqual(1);
		}

		public class when_no_dispatch_items_are_in_the_batch
		{
			Because of = () =>
				handler.OnNext(Create(0), 1, true);

			It should_NOT_invoke_the_callback_for_the_highest_dispatch_item = () =>
				acknowledged.ShouldEqual(0);

			It should_NOT_invoke_the_acknowledgement_callback = () =>
				invocations.ShouldEqual(0);
		}

		Establish context = () =>
		{
			acknowledged = 0;
			invocations = 0;
			handler = new AcknowledgementHandler();
		};

		static JournalItem Create(int sequence)
		{
			if (sequence == 0)
				return new JournalItem();

			return new JournalItem
			{
				Acknowledgement = () =>
				{
					invocations++;
					acknowledged = sequence;
				}
			};
		}

		static AcknowledgementHandler handler;
		static int acknowledged;
		static int invocations;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
