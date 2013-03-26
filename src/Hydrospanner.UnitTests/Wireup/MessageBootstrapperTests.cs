﻿#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using NSubstitute;
	using Persistence;
	using Phases;
	using Phases.Journal;
	using Phases.Transformation;

	[Subject(typeof(MessageBootstrapper))]
	public class when_initializing_the_message_bootstrapper
	{
		public class and_constructor_parameters_are_null
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

		public class and_constructor_parameters_are_NOT_null
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
				Catch.Exception(() => bootstrapper.Restore(null, Disruptor, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_when_the_ring_is_null = () =>
				Catch.Exception(() => bootstrapper.Restore(info, null, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_when_the_repository_is_null = () =>
				Catch.Exception(() => bootstrapper.Restore(info, Disruptor, null)).ShouldBeOfType<ArgumentNullException>();

			static readonly IDisruptor<JournalItem> Disruptor = new DisruptorBase<JournalItem>(null);
		}

		public class during_message_restoration
		{
			public class when_the_application_is_completely_caught_up_with_dispatching_and_transformations
			{
				Establish context = () =>
				{
					store.Load(Arg.Any<long>()).Returns(new JournaledMessage[0]);
					transformationRing = Substitute.For<IDisruptor<TransformationItem>>();
					transformationRingBuffer = new RingBufferHarness<TransformationItem>();
					transformationRing.RingBuffer.Returns(transformationRingBuffer.RingBuffer);
					factory.CreateStartupTransformationDisruptor(repository, info, Arg.Any<Action>()).Returns(transformationRing);

					journalRing = Substitute.For<IDisruptor<JournalItem>>();
					journalRingBuffer = new RingBufferHarness<JournalItem>();
					journalRing.RingBuffer.Returns(journalRingBuffer.RingBuffer);
				};

				Because of = () =>
					bootstrapper.Restore(info, journalRing, repository);

				It should_NOT_publish_any_items_to_the_journal_ring_to_be_dispatched = () =>
					journalRingBuffer.AllItems.ShouldBeEmpty();

				It should_NOT_publish_any_items_to_the_transformation_ring = () =>
					transformationRingBuffer.AllItems.ShouldBeEmpty();

				Cleanup after = () =>
				{
					transformationRingBuffer = transformationRingBuffer.TryDispose();
					transformationRing = null;
					journalRing = null;
					journalRingBuffer = journalRingBuffer.TryDispose();
				};

				static IDisruptor<TransformationItem> transformationRing;
				static RingBufferHarness<TransformationItem> transformationRingBuffer;
				static IDisruptor<JournalItem> journalRing;
				static RingBufferHarness<JournalItem> journalRingBuffer;
			}

			public class when_there_are_messages_that_need_to_be_dispatched
			{
				Establish context = () =>
				{
					info.DispatchSequence = 41;
					info.SnapshotSequence = int.MaxValue;
					message = new JournaledMessage { Sequence = 42 };
					store.Load(41).Returns(new List<JournaledMessage> { message });

					journalRing = Substitute.For<IDisruptor<JournalItem>>();
					journalRingBuffer = new RingBufferHarness<JournalItem>();
					journalRing.RingBuffer.Returns(journalRingBuffer.RingBuffer);
				};

				Because of = () =>
				{
					bootstrapper.Restore(info, journalRing, repository);
					Thread.Sleep(100);
				};

				Cleanup after = () =>
				{
					journalRing = null;
					journalRingBuffer = journalRingBuffer.TryDispose();
				};

				It should_send_each_to_the_journal_ring_to_be_dispatched = () =>
					journalRingBuffer.AllItems.Single()
						.ShouldBeLike(new JournalItem { MessageSequence = 42, ItemActions = JournalItemAction.Dispatch });

				static JournaledMessage message;
				static IDisruptor<JournalItem> journalRing;
				static RingBufferHarness<JournalItem> journalRingBuffer;
			}

			public class when_there_are_messages_that_require_additional_transformations
			{
				Establish context = () =>
				{
					itemCount = 1;
					info.SnapshotSequence = 41;
					info.DispatchSequence = int.MaxValue;
					message = new JournaledMessage { Sequence = 42 };
					store.Load(41).Returns(new List<JournaledMessage> { message });

					transformationRing = Substitute.For<IDisruptor<TransformationItem>>();
					transformationRingBuffer = new RingBufferHarness<TransformationItem>(CompleteCallback);
					transformationRing.RingBuffer.Returns(transformationRingBuffer.RingBuffer);
					factory.CreateStartupTransformationDisruptor(repository, info, Arg.Do<Action>(x => completeCallback = x)).Returns(transformationRing);

					journalRing = Substitute.For<IDisruptor<JournalItem>>();
					journalRingBuffer = new RingBufferHarness<JournalItem>();
					journalRing.RingBuffer.Returns(journalRingBuffer.RingBuffer);
				};

				Because of = () =>
					bootstrapper.Restore(info, journalRing, repository);

				It should_create_a_transformation_disruptor = () =>
					factory.CreateStartupTransformationDisruptor(repository, info, Arg.Any<Action>()).Received(1);

				It should_publish_the_messages_to_the_newly_created_disruptor = () =>
					transformationRingBuffer.AllItems.Single().ShouldBeLike(new TransformationItem
					{
						MessageSequence = 42,
						IsDocumented = true,
						IsLocal = true
					});

				It should_shutdown_the_transformation_disruptor_after_everything_is_processed = () =>
					transformationRing.Received(1).Dispose();

				Cleanup after = () =>
				{
					transformationRingBuffer = transformationRingBuffer.TryDispose();
					transformationRing = null;
					journalRing = null;
					journalRingBuffer = journalRingBuffer.TryDispose();
				};

				static void CompleteCallback(TransformationItem item)
				{
					if (++count == itemCount)
						completeCallback();
				}

				static int count;
				static int itemCount;
				static Action completeCallback;
				
				static JournaledMessage message;

				static IDisruptor<TransformationItem> transformationRing;
				static RingBufferHarness<TransformationItem> transformationRingBuffer;

				static IDisruptor<JournalItem> journalRing;
				static RingBufferHarness<JournalItem> journalRingBuffer;
			}

			public class when_there_are_messages_that_need_to_be_dispatched_and_that_require_additional_transformations
			{
				Establish context = () =>
				{
					itemCount = 2;
					info.SnapshotSequence = 40;
					info.DispatchSequence = 40;
					message1 = new JournaledMessage { Sequence = 41 };
					message2 = new JournaledMessage { Sequence = 42 };
					store.Load(40).Returns(new List<JournaledMessage> { message1, message2 });

					transformationRing = Substitute.For<IDisruptor<TransformationItem>>();
					transformationRingBuffer = new RingBufferHarness<TransformationItem>(CompleteCallback);
					transformationRing.RingBuffer.Returns(transformationRingBuffer.RingBuffer);
					factory.CreateStartupTransformationDisruptor(repository, info, Arg.Do<Action>(x => completeCallback = x)).Returns(transformationRing);

					journalRing = Substitute.For<IDisruptor<JournalItem>>();
					journalRingBuffer = new RingBufferHarness<JournalItem>();
					journalRing.RingBuffer.Returns(journalRingBuffer.RingBuffer);
				};

				Because of = () =>
				{
					bootstrapper.Restore(info, journalRing, repository);
					Thread.Sleep(100); // let ring buffer catch up
				};

				It should_send_each_message_that_should_be_dispatched_to_the_journal_ring_to_be_dispatched = () =>
					journalRingBuffer.AllItems.ShouldBeLike(new[] 
					{ 
						new JournalItem { MessageSequence = 41, ItemActions = JournalItemAction.Dispatch },
						new JournalItem { MessageSequence = 42, ItemActions = JournalItemAction.Dispatch }
					});

				It should_create_a_transformation_disruptor = () =>
					factory.CreateStartupTransformationDisruptor(repository, info, Arg.Any<Action>()).Received(1);

				It should_shutdown_the_transformation_disruptor_after_everything_is_processed = () =>
					transformationRing.Received(1).Dispose();

				It should_publish_the_messages_requiring_transformation_to_the_newly_created_disruptor = () =>
					transformationRingBuffer.AllItems.ShouldBeLike(new[]
					{
						new TransformationItem { MessageSequence = 41, IsDocumented = true, IsLocal = true },
						new TransformationItem { MessageSequence = 42, IsDocumented = true, IsLocal = true }
					});

				Cleanup after = () =>
				{
					transformationRingBuffer = transformationRingBuffer.TryDispose();
					transformationRing = null;
					journalRing = null;
					journalRingBuffer = journalRingBuffer.TryDispose();
				};

				static void CompleteCallback(TransformationItem item)
				{
					if (++count == itemCount)
						completeCallback();
				}

				static int count;
				static int itemCount;
				static Action completeCallback;

				static JournaledMessage message1;
				static JournaledMessage message2;

				static IDisruptor<TransformationItem> transformationRing;
				static RingBufferHarness<TransformationItem> transformationRingBuffer;

				static IDisruptor<JournalItem> journalRing;
				static RingBufferHarness<JournalItem> journalRingBuffer;
			}
		}

		Establish context = () =>
		{
			info = new BootstrapInfo();
			store = Substitute.For<IMessageStore>();
			factory = Substitute.For<DisruptorFactory>();
			bootstrapper = new MessageBootstrapper(store, factory);

			repository = Substitute.For<IRepository>();
		};

		Cleanup after = () =>
		{
			info = null;
			store = null;
			factory = null;
			bootstrapper = null;
			repository = null;
		};
		
		static BootstrapInfo info;
		static DisruptorFactory factory;
		static IMessageStore store;
		static MessageBootstrapper bootstrapper;
		static IRepository repository;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169