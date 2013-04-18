#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using NSubstitute;
	using Persistence;
	using Phases.Journal;
	using Phases.Transformation;

	[Subject(typeof(MessageBootstrapper))]
	public class when_initializing_the_message_bootstrapper
	{
		public class and_constructor_parameters_are_invalid
		{
			Establish context = () =>
			{
				store = Substitute.For<IMessageStore>();
				disruptorFactory = Substitute.For<DisruptorFactory>();
			};

			It should_throw_if_the_message_store_is_null = () =>
				Catch.Exception(() => new MessageBootstrapper(null, disruptorFactory)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_disruptor_factory_is_null = () =>
				Catch.Exception(() => new MessageBootstrapper(store, null)).ShouldBeOfType<ArgumentNullException>();

			static IMessageStore store;
			static DisruptorFactory disruptorFactory;
		}

		public class and_constructor_parameters_are_valid
		{
			It should_NOT_throw = () =>
				Catch.Exception(() => new MessageBootstrapper(
					Substitute.For<IMessageStore>(), Substitute.For<DisruptorFactory>())).ShouldBeNull();
		}
	}

	[Subject(typeof(MessageBootstrapper))]
	public class when_restoring_during_bootstrapping
	{
		public class when_input_parameters_are_null
		{
			It should_throw_when_the_info_is_null = () =>
				Catch.Exception(() => bootstrapper.Restore(null, journal, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_when_the_ring_is_null = () =>
				Catch.Exception(() => bootstrapper.Restore(info, null, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_when_the_repository_is_null = () =>
				Catch.Exception(() => bootstrapper.Restore(info, journal, null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class during_message_restoration
		{
			public class when_the_application_is_completely_caught_up_with_dispatching_and_transformations
			{
				Establish context = () =>
				{
					store.Load(Arg.Any<long>()).Returns(new JournaledMessage[0]);
					factory
						.CreateStartupTransformationDisruptor(
							repository, 
							Arg.Is<BootstrapInfo>(x => x.JournaledSequence - x.SnapshotSequence == 0), 
							Arg.Do<Action<bool>>(x => completeCallback = x))
						.Returns(default(DisruptorHarness<TransformationItem>));
				};

				Because of = () =>
					bootstrapper.Restore(info, journal, repository);

				It should_NOT_publish_any_items_to_the_journal_ring_to_be_dispatched = () =>
					journal.Ring.AllItems.ShouldBeEmpty();

				It should_NOT_publish_any_items_to_the_transformation_ring = () =>
					transformation.Ring.AllItems.ShouldBeEmpty();

				It should_NOT_attempt_to_start_the_transformation_ring = () =>
					transformation.Started.ShouldBeFalse();
			}

			public class when_there_are_messages_that_need_to_be_dispatched
			{
				Establish context = () =>
				{
					info.DispatchSequence = 41;
					info.SnapshotSequence = int.MaxValue;
					var local = new JournaledMessage { Sequence = 42 };
					var foreign = new JournaledMessage { Sequence = 43, ForeignId = Guid.NewGuid() };
					store.Load(info.DispatchSequence + 1).Returns(new List<JournaledMessage> { local, foreign });
				};

				Because of = () =>
					bootstrapper.Restore(info, journal, repository);

				It should_send_each_to_the_journal_ring_to_be_dispatched = () =>
					journal.Ring.AllItems.Single()
						.ShouldBeLike(new JournalItem { MessageSequence = 42, ItemActions = JournalItemAction.Dispatch });

				It should_NOT_send_any_foreign_messages_to_be_redispatched = () =>
					journal.Ring.AllItems.Count.ShouldEqual(1);
			}

			public class when_there_are_messages_that_need_to_be_dispatched_and_that_require_additional_transformations
			{
				Establish context = () =>
				{
					itemCount = 2;
					const long Checkpoint = 40;
					info.SnapshotSequence = Checkpoint;
					info.DispatchSequence = Checkpoint;
					message1 = new JournaledMessage { Sequence = 41 };
					message2 = new JournaledMessage { Sequence = 42 };
					store.Load(Checkpoint + 1).Returns(new List<JournaledMessage> { message1, message2 });
				};

				Because of = () =>
					bootstrapper.Restore(info, journal, repository);

				It should_send_each_message_that_should_be_dispatched_to_the_journal_ring_to_be_dispatched = () =>
					journal.Ring.AllItems.ShouldBeLike(new[] 
					{
						new JournalItem { MessageSequence = 41, ItemActions = JournalItemAction.Dispatch },
						new JournalItem { MessageSequence = 42, ItemActions = JournalItemAction.Dispatch }
					});

				It should_create_a_transformation_disruptor = () =>
					factory.Received(1).CreateStartupTransformationDisruptor(repository, info, Arg.Any<Action<bool>>());

				It should_shutdown_the_transformation_disruptor_after_everything_is_processed = () =>
					transformation.Disposed.ShouldBeTrue();

				It should_publish_the_messages_requiring_transformation_to_the_newly_created_disruptor = () =>
					transformation.Ring.AllItems.ShouldBeLike(new[]
					{
						new TransformationItem { MessageSequence = 41 },
						new TransformationItem { MessageSequence = 42 }
					});

				static JournaledMessage message1;
				static JournaledMessage message2;
			}

			public class when_there_are_messages_that_require_additional_transformations
			{
				Establish context = () =>
				{
					itemCount = 1;
					info.SnapshotSequence = 41;
					info.DispatchSequence = int.MaxValue;
					message = new JournaledMessage { Sequence = 42 };
					store.Load(info.SnapshotSequence + 1).Returns(new List<JournaledMessage> { message });
				};

				Because of = () =>
					bootstrapper.Restore(info, journal, repository);

				It should_create_a_transformation_disruptor = () =>
					factory.Received(1).CreateStartupTransformationDisruptor(repository, info, Arg.Any<Action<bool>>());

				It should_start_the_transformation_disruptor = () =>
					transformation.Started.ShouldBeTrue();

				It should_publish_the_messages_to_the_newly_created_disruptor = () =>
					transformation.Ring.AllItems.Single().ShouldBeLike(new TransformationItem { MessageSequence = 42 });

				It should_shutdown_the_transformation_disruptor_after_everything_is_processed = () =>
					transformation.Disposed.ShouldBeTrue();

				static JournaledMessage message;
			}
		}

		Establish context = () =>
		{
			info = new BootstrapInfo();
			store = Substitute.For<IMessageStore>();
			factory = Substitute.For<DisruptorFactory>();
			bootstrapper = new MessageBootstrapper(store, factory);

			repository = Substitute.For<IRepository>();
			transformation = new DisruptorHarness<TransformationItem>(CompleteCallback);
			factory.CreateStartupTransformationDisruptor(repository, info, Arg.Do<Action<bool>>(x => completeCallback = x))
				.Returns(transformation);
			journal = new DisruptorHarness<JournalItem>();
		};

		Cleanup after = () =>
		{
			info = null;
			store = null;
			factory = null;
			bootstrapper = null;
			repository = null;
			transformation = null;
			journal = null;
		};

		static void CompleteCallback()
		{
			if (++count == itemCount)
				completeCallback(true);
		}

		static int count;
		static int itemCount;
		static Action<bool> completeCallback;
		static BootstrapInfo info;
		static DisruptorFactory factory;
		static IMessageStore store;
		static MessageBootstrapper bootstrapper;
		static IRepository repository;
		static DisruptorHarness<TransformationItem> transformation;
		static DisruptorHarness<JournalItem> journal; 
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
