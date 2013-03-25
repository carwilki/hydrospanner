#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Machine.Specifications;
	using Transformation;

	[Subject(typeof(CountdownHandler))]
	public class when_counting_the_number_of_received_messages
	{
		public class when_the_countdown_value_is_not_positive
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new CountdownHandler(0, Callback)).ShouldBeOfType<ArgumentOutOfRangeException>();
		}
		public class when_no_callback_is_provided
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new CountdownHandler(1, null)).ShouldBeOfType<ArgumentNullException>();
		}
		public class when_all_bootstrap_items_have_been_received
		{
			Establish context = () =>
				handler = new CountdownHandler(1, Callback);

			Because of = () =>
				handler.OnNext((BootstrapItem)null, 0, false);

			It should_invoke_the_provided_callback = () =>
				calls.ShouldEqual(1);
		}
		public class when_all_transformation_items_have_been_received
		{
			Establish context = () =>
				handler = new CountdownHandler(1, Callback);

			Because of = () =>
				handler.OnNext((TransformationItem)null, 0, false);

			It should_invoke_the_provided_callback = () =>
				calls.ShouldEqual(1);
		}
		public class when_more_than_expected_number_of_bootstrap_items_has_been_received
		{
			Establish context = () =>
			{
				handler = new CountdownHandler(1, Callback);
				handler.OnNext((BootstrapItem)null, 0, false); // calls = 1
			};

			Because of = () =>
				handler.OnNext((BootstrapItem)null, 0, false); // doesn't increment calls

			It should_invoke_the_provided_callback_exactly_once = () =>
				calls.ShouldEqual(1);
		}
		public class when_more_than_expected_number_of_transformation_items_has_been_received
		{
			Establish context = () =>
			{
				handler = new CountdownHandler(1, Callback);
				handler.OnNext((TransformationItem)null, 0, false); // calls = 1
			};

			Because of = () =>
				handler.OnNext((TransformationItem)null, 0, false); // doesn't increment calls

			It should_invoke_the_provided_callback_exactly_once = () =>
				calls.ShouldEqual(1);
		}

		Establish context = () =>
			calls = 0;

		static readonly Action Callback = () => calls++;
		static CountdownHandler handler;
		static int calls;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
