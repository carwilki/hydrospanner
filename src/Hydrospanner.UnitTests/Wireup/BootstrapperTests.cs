#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using Machine.Specifications;
	using NSubstitute;
	using NSubstitute.Experimental;
	using Persistence;
	using Phases.Journal;
	using Phases.Snapshot;
	using Phases.Transformation;

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

			It should_get_everything_going = () =>
				Received.InOrder(ExpectedStartCalls);
		}

		public class when_disposing_the_bootstrapper_before_it_is_started
		{
			Establish context = () =>
				ThreadExtensions.Freeze(x => { });

			Because of = () =>
				bootstrapper.Dispose();

			It should_do_NOTHING = () =>
				Received.InOrder(() => { });
		}

		public class when_disposing_a_started_bootstrapper
		{
			Establish context = () =>
				bootstrapper.Start();

			Because of = () =>
				bootstrapper.Dispose();

			It should_allow_time_for_the_disruptors_to_shutdown = () =>
				naps.ShouldBeLike(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(500) });

			It should_dispose_of_all_disposable_resources = () =>
				Received.InOrder(ExpectedDisposal);
		}

		public class when_disposing_a_disposed_bootstrapper
		{
			Establish context = () =>
			{
				bootstrapper.Start();
				bootstrapper.Dispose();
			};

			Because of = () =>
				bootstrapper.Dispose();

			It should_NOT_try_to_dispose_the_resoures_a_second_time = () =>
				Received.InOrder(() =>
				{
					ExpectedStartCalls(); // Start
					ExpectedDisposal();   // First disposal
				});
		}

		public class when_starting_a_started_bootstrapper
		{
			Establish context = () =>
				bootstrapper.Start();

			Because of = () =>
				bootstrapper.Start();

			It should_NOT_try_to_bootstrap_the_system_a_second_time = () =>
				Received.InOrder(ExpectedStartCalls); // only one time
		}

		Establish context = () =>
		{
			InstantiateStuff();
			SetupStuff();
		};

		static void InstantiateStuff()
		{
			info = new BootstrapInfo(42, 24, new string[0], new Collection<Guid>());
			info2 = new BootstrapInfo(43, 25, new[] { "blah" }, new Collection<Guid>());
			repository = Substitute.For<IRepository>();
			disruptors = Substitute.For<DisruptorFactory>();
			persistence = Substitute.For<PersistenceBootstrapper>();
			snapshots = Substitute.For<SnapshotBootstrapper>();
			messages = Substitute.For<MessageBootstrapper>();
			messaging = Substitute.For<MessagingFactory>();

			journalDisruptor = Substitute.For<IDisruptor<JournalItem>>();
			snapshotDisruptor = Substitute.For<IDisruptor<SnapshotItem>>();
			transformationDisruptor = Substitute.For<IDisruptor<TransformationItem>>();
			transformationRingBuffer = new TestRingBuffer<TransformationItem>();
			listener = Substitute.For<MessageListener>();

			bootstrapper = new Bootstrapper(repository, disruptors, persistence, snapshots, messages, messaging);
			naps = new List<TimeSpan>();
		}

		static void SetupStuff()
		{
			ThreadExtensions.Freeze(x => naps.Add(x));
			persistence.Restore().Returns(info);
			snapshots.RestoreSnapshots(info, repository).Returns(info2);
			disruptors.CreateJournalDisruptor(info2).Returns(journalDisruptor);
			disruptors.CreateSnapshotDisruptor().Returns(snapshotDisruptor);
			disruptors.CreateTransformationDisruptor().Returns(transformationDisruptor);

			transformationDisruptor.RingBuffer.Returns(transformationRingBuffer);
			messaging.CreateMessageListener(transformationRingBuffer).Returns(listener);
		}

		static void ExpectedStartCalls()
		{
			persistence.Restore();
			snapshots.RestoreSnapshots(info, repository);

			disruptors.CreateJournalDisruptor(info2);
			disruptors.CreateSnapshotDisruptor();
			disruptors.CreateTransformationDisruptor();

			journalDisruptor.Start();
			snapshotDisruptor.Start();
			messages.Restore(info2, journalDisruptor, repository);

			transformationDisruptor.Start();

			messaging.CreateMessageListener(transformationRingBuffer);
		}

		static void ExpectedDisposal()
		{
			listener.Dispose();
			transformationDisruptor.Dispose();
			snapshotDisruptor.Dispose();
			journalDisruptor.Dispose();
		}

		static BootstrapInfo info;
		static BootstrapInfo info2;
		static Bootstrapper bootstrapper;
		static IRepository repository;
		static DisruptorFactory disruptors;
		static PersistenceBootstrapper persistence;
		static SnapshotBootstrapper snapshots;
		static MessageBootstrapper messages;
		static MessagingFactory messaging;

		static IDisruptor<JournalItem> journalDisruptor;
		static IDisruptor<SnapshotItem> snapshotDisruptor;
		static IDisruptor<TransformationItem> transformationDisruptor;
		static TestRingBuffer<TransformationItem> transformationRingBuffer;
		static MessageListener listener;
		static List<TimeSpan> naps;
	}
}
// ReSharper restore InconsistentNaming
#pragma warning restore 169