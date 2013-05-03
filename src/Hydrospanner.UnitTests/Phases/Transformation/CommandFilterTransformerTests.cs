#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(CommandFilterTransformer))]
	public class when_initializing_the_command_filter
	{
		It should_throw_when_null_is_provided = () =>
			Catch.Exception(() => new CommandFilterTransformer(null)).ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(CommandFilterTransformer))]
	public class when_a_delivery_is_received
	{
		Establish context = () =>
		{
			inner = Substitute.For<ITransformer>();
			inner.Transform(delivery).Returns(x => new[] { "42" });
			transformer = new CommandFilterTransformer(inner);
		};

		Because of = () =>
			result = transformer.Transform(delivery);

		It should_provide_the_delivery_to_the_underlying_handler = () =>
			result.Single().ShouldEqual("42");

		static readonly Delivery<int> delivery = new Delivery<int>(0, new Dictionary<string, string>(), 1, true, true); 
		static CommandFilterTransformer transformer;
		static ITransformer inner;
		static IEnumerable<object> result;
	}

	[Subject(typeof(CommandFilterTransformer))]
	public class when_a_live_command_messagedelivery_is_received
	{
		Establish context = () =>
		{
			inner = Substitute.For<ITransformer>();
			inner.Transform(delivery).Returns(x => new[] { "42" });
			transformer = new CommandFilterTransformer(inner);
		};

		Because of = () =>
			result = transformer.Transform(delivery);

		It should_provide_the_delivery_to_the_underlying_handler = () =>
			result.Single().ShouldEqual("42");

		static readonly Delivery<SomeCommand> delivery = new Delivery<SomeCommand>(new SomeCommand(), new Dictionary<string, string>(), 1, true, true);
		static CommandFilterTransformer transformer;
		static ITransformer inner;
		static IEnumerable<object> result;

		private class SomeCommand { }
	}

	[Subject(typeof(CommandFilterTransformer))]
	public class when_a_replay_command_delivery_is_received
	{
		Establish context = () =>
		{
			inner = Substitute.For<ITransformer>();
			transformer = new CommandFilterTransformer(inner);
		};

		Because of = () =>
			result = transformer.Transform(delivery);

		It should_NOT_provide_the_delivery_to_the_underlying_handler = () =>
			inner.Received(0).Transform(Arg.Any<Delivery<SomeCommand>>());

		It should_return_an_empty_set = () =>
			result.ShouldBeEmpty();

		const bool Live = false;
		static readonly Delivery<SomeCommand> delivery = new Delivery<SomeCommand>(new SomeCommand(), new Dictionary<string, string>(), 1, Live, true);
		static CommandFilterTransformer transformer;
		static ITransformer inner;
		static IEnumerable<object> result; 

		private class SomeCommand { }
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414