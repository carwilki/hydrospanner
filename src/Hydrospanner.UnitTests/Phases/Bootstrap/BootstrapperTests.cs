#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Collections.ObjectModel;
	using Configuration;
	using Machine.Specifications;
	using NSubstitute;
	using Persistence;

	[Subject(typeof(Bootstrapper))]
	public class when_instantiating_the_bootstrapper
	{
		public class with_null_values
		{
			It should_throw_if_the_repository_is_null = () =>
				Try(() => new Bootstrapper(null, disruptors, persistence, snapshots, messages, messaging)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_disruptors_are_null = () =>
				Try(() => new Bootstrapper(repository, null, persistence, snapshots, messages, messaging)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_persistence_is_null = () =>
				Try(() => new Bootstrapper(repository, disruptors, null, snapshots, messages, messaging)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_snapshots_are_null = () =>
				Try(() => new Bootstrapper(repository, disruptors, persistence, null, messages, messaging)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_messages_are_null = () =>
				Try(() => new Bootstrapper(repository, disruptors, persistence, snapshots, null, messaging)).ShouldBeOfType<ArgumentNullException>();
			
			It should_throw_if_the_messaging_is_null = () =>
				Try(() => new Bootstrapper(repository, disruptors, persistence, snapshots, messages, null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class with_valid_values
		{
			It should_NOT_throw = () =>
				Try(() => new Bootstrapper(repository, disruptors, persistence, snapshots, messages, messaging)).ShouldBeNull();
		}

		static Exception Try(Action action)
		{
			return Catch.Exception(action);
		}

		Establish context = () =>
		{
			repository = Substitute.For<IRepository>();
			disruptors = Substitute.For<DisruptorFactory>();
			persistence = Substitute.For<PersistenceBootstrapper>();
			snapshots = Substitute.For<SnapshotBootstrapper>();
			messages = Substitute.For<MessageBootstrapper>();
			messaging = Substitute.For<MessagingFactory>();
		};

		static IRepository repository;
		static DisruptorFactory disruptors;
		static PersistenceBootstrapper persistence;
		static SnapshotBootstrapper snapshots;
		static MessageBootstrapper messages;
		static MessagingFactory messaging;
	}

	[Subject(typeof(Bootstrapper))]
	public class when_bootstrapping
	{
		public class when_starting_the_bootstrapper
		{
			Because of = () =>
				bootstrapper.Start();


		}

		Establish context = () =>
		{
			info = new BootstrapInfo(42, 24, new string[0], new Collection<Guid>());
			repository = Substitute.For<IRepository>();
			disruptors = Substitute.For<DisruptorFactory>();
			persistence = Substitute.For<PersistenceBootstrapper>();
			snapshots = Substitute.For<SnapshotBootstrapper>();
			messages = Substitute.For<MessageBootstrapper>();
			messaging = Substitute.For<MessagingFactory>();
			bootstrapper = new Bootstrapper(repository, disruptors, persistence, snapshots, messages, messaging);
		};

		static BootstrapInfo info;
		static Bootstrapper bootstrapper;
		static IRepository repository;
		static DisruptorFactory disruptors;
		static PersistenceBootstrapper persistence;
		static SnapshotBootstrapper snapshots;
		static MessageBootstrapper messages;
		static MessagingFactory messaging;
	}
}
// ReShaMessagingFactory messaging)rper restore InconsistentNaming
#pragma warning restore 169
