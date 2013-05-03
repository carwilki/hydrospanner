#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(ReflectionDeliveryHandler))]
	public class with_the_reflection_delivery_handler
	{
		public class when_initializing
		{
			It should_throw_an_exception_when_a_null_transformer_is_provided = () =>
				Catch.Exception(() => new ReflectionDeliveryHandler(null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class when_sending_a_delivery_to_the_underlying_handler
		{
			Establish context = () =>
			{
				item = new TransformationItem();
				item.AsLocalMessage(42, "Hello, World!", new Dictionary<string, string>());

				var transformer = Substitute.For<ITransformer>();
				transformer.Transform(Arg.Do<Delivery<string>>(x => delivery = x));
				handler = new ReflectionDeliveryHandler(transformer);
			};

			Because of = () =>
				handler.Deliver(item, false);

			It should_preserve_the_sequence = () =>
				delivery.Sequence.ShouldEqual(item.MessageSequence);

			It should_preserve_the_body = () =>
				delivery.Message.ShouldEqual(item.Body);

			It should_preserve_the_headers = () =>
				delivery.Headers.ShouldEqual(item.Headers);

			It should_indicate_the_correct_replay_status = () =>
				delivery.Live.ShouldEqual(false);

			It should_preserve_the_locality = () =>
				delivery.Local.ShouldEqual(true);

			static TransformationItem item;
			static Delivery<string> delivery;
			static ReflectionDeliveryHandler handler;
		}

		public class when_sending_a_replayed_foreign_message_to_the_underlying_handler
		{
			Establish context = () =>
			{
				item = new TransformationItem();
				item.AsLocalMessage(42, "Hello, World!", new Dictionary<string, string>());
				item.ForeignId = Guid.NewGuid(); // make it look foreign

				var transformer = Substitute.For<ITransformer>();
				transformer.Transform(Arg.Do<Delivery<string>>(x => delivery = x));
				handler = new ReflectionDeliveryHandler(transformer);
			};

			Because of = () =>
				handler.Deliver(item, true);

			It should_preserve_the_sequence = () =>
				delivery.Sequence.ShouldEqual(item.MessageSequence);

			It should_preserve_the_body = () =>
				delivery.Message.ShouldEqual(item.Body);

			It should_preserve_the_headers = () =>
				delivery.Headers.ShouldEqual(item.Headers);

			It should_indicate_the_correct_live_status = () =>
				delivery.Live.ShouldEqual(true);

			It should_indicate_the_item_as_foreign = () =>
				delivery.Local.ShouldEqual(false);

			static TransformationItem item;
			static Delivery<string> delivery;
			static ReflectionDeliveryHandler handler;
		}

		public class when_replaying_a_locally_generated_message
		{
			Establish context = () =>
			{
				var transformer = Substitute.For<ITransformer>();
				transformer.Transform(Arg.Do<Delivery<string>>(x => delivery = x));
				handler = new ReflectionDeliveryHandler(transformer);
			};

			Because of = () =>
				handler.Deliver("Hello, World!", 42);

			It should_preserve_the_sequence = () =>
				delivery.Sequence.ShouldEqual(42);

			It should_preserve_the_body = () =>
				delivery.Message.ShouldEqual("Hello, World!");

			It should_preserve_set_empty_headers = () =>
				delivery.Headers.ShouldBeEmpty();

			It should_indicate_the_correct_live_status = () =>
				delivery.Live.ShouldEqual(true);

			It should_indicate_the_item_as_local = () =>
				delivery.Local.ShouldEqual(true);

			static Delivery<string> delivery;
			static ReflectionDeliveryHandler handler;
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414