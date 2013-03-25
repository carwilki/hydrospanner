#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Journal;
	using Machine.Specifications;
	using NSubstitute;
	using Persistence;

	[Subject(typeof(MessageBootstrapper))]
	public class when_initializing_the_message_bootstrapper
	{
		public class and_constructor_parameters_are_null
		{
			It should_throw_if_the_message_store_is_null = () =>
				Catch.Exception(() => new MessageBootstrapper(null, Substitute.For<DisruptorFactory>())).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_disruptor_factory_is_null = () =>
				Catch.Exception(() => new MessageBootstrapper(Substitute.For<IMessageStore>(), null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class and_constructor_parameters_are_NOT_null
		{
			It should_NOT_throw = () =>
				Catch.Exception(() => new MessageBootstrapper(Substitute.For<IMessageStore>(), Substitute.For<DisruptorFactory>())).ShouldBeNull();
		}
	}

	[Subject(typeof(MessageBootstrapper))]
	public class when_attempting_to_restore_using_null_input_parameters
	{
		It should_throw_when_the_info_is_null = () =>
			Catch.Exception(() => bootstrapper.Restore(null, journalRing, repository)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_when_the_ring_is_null = () =>
			Catch.Exception(() => bootstrapper.Restore(info, null, repository)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_when_the_repository_is_null = () =>
			Catch.Exception(() => bootstrapper.Restore(info, journalRing, null)).ShouldBeOfType<ArgumentNullException>();

		Establish context = () =>
		{
			repository = Substitute.For<IRepository>();
			journalRing = Substitute.For<IDisruptor<JournalItem>>();
			info = new BootstrapInfo();
			store = Substitute.For<IMessageStore>();
			factory = Substitute.For<DisruptorFactory>();
			bootstrapper = new MessageBootstrapper(store, factory);
		};

		static DisruptorFactory factory;
		static IMessageStore store;
		static IRepository repository;
		static IDisruptor<JournalItem> journalRing;
		static BootstrapInfo info;
		static MessageBootstrapper bootstrapper;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
