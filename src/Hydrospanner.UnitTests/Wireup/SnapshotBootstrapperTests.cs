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
	using Phases.Bootstrap;
	using Phases.Snapshot;

	[Subject(typeof(SnapshotBootstrapper))]
	public class when_bootstrapping_from_a_system_snapshot
	{
		public class when_the_snapshot_factory_provided_is_null
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new SnapshotBootstrapper(null, Substitute.For<DisruptorFactory>())).ShouldBeOfType<ArgumentNullException>();
		}
		public class when_the_disruptor_factory_provided_is_null
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new SnapshotBootstrapper(Substitute.For<SnapshotFactory>(), null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class when_no_bootstrap_info_is_provided
		{
			Because of = () =>
				Try(() => bootstrapper.RestoreSnapshots(null, repository));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_no_repository_is_provided
		{
			Because of = () =>
				Try(() => bootstrapper.RestoreSnapshots(providedInfo, null));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_streaming_in_a_system_snapshot
		{
			Establish context = () =>
			{
				count = 0;
				completeCallback = null;

				reader = Substitute.For<SystemSnapshotStreamReader>();
				reader.Read().Returns(new[]
				{
					new KeyValuePair<string, byte[]>("1", new byte[] { 1 }), 
					new KeyValuePair<string, byte[]>("2", new byte[] { 2 }), 
					new KeyValuePair<string, byte[]>("3", new byte[] { 3 })
				});

				disruptor = new DisruptorHarness<BootstrapItem>(CompleteCallback);

				reader.Count.Returns(ItemCount);
				reader.MessageSequence.Returns(providedInfo.JournaledSequence - 1);

				snapshots.CreateSystemSnapshotStreamReader(providedInfo.JournaledSequence).Returns(reader);
				disruptors.CreateBootstrapDisruptor(repository, ItemCount, Arg.Do<Action>(x => completeCallback = x)).Returns(disruptor);
			};
			static void CompleteCallback()
			{
				if (++count == ItemCount)
					completeCallback();
			}

			Because of = () =>
				returnedInfo = bootstrapper.RestoreSnapshots(providedInfo, repository);

			It should_load_from_the_journaled_sequence = () =>
				snapshots.Received(1).CreateSystemSnapshotStreamReader(providedInfo.JournaledSequence);

			It should_create_a_disruptor = () =>
				disruptors.Received(1).CreateBootstrapDisruptor(repository, ItemCount, Arg.Any<Action>());

			It should_start_the_disruptor = () =>
				disruptor.Started.ShouldBeTrue();

			It should_publish_each_item_to_the_journal = () =>
				count.ShouldEqual(ItemCount);

			It should_dispose_the_underlying_snapshot_reader = () =>
				reader.Received(1).Dispose();

			It should_dispose_the_disruptor = () =>
				disruptor.Disposed.ShouldBeTrue();

			It should_return_the_bootstrap_info_augmented_with_the_current_snapshot_sequence = () =>
				returnedInfo.ShouldBeLike(new BootstrapInfo(
					providedInfo.JournaledSequence,
					providedInfo.DispatchSequence,
					providedInfo.SerializedTypes,
					providedInfo.DuplicateIdentifiers)
						.AddSnapshotSequence(reader.MessageSequence));

			const int ItemCount = 3;
			static SystemSnapshotStreamReader reader;
			static DisruptorHarness<BootstrapItem> disruptor;
			static int count;
			static Action completeCallback;
		}

		public class when_latest_snapshot_does_not_contain_any_items
		{
			Establish context = () =>
			{
				reader = Substitute.For<SystemSnapshotStreamReader>();
				reader.Count.Returns(0);
				reader.MessageSequence.Returns(providedInfo.JournaledSequence - 1);

				snapshots.CreateSystemSnapshotStreamReader(providedInfo.JournaledSequence).Returns(reader);
			};

			Because of = () =>
				returnedInfo = bootstrapper.RestoreSnapshots(providedInfo, repository);

			It should_not_create_a_disruptor = () =>
				disruptors.ReceivedCalls().Count().ShouldEqual(0);

			It should_return_the_bootstrap_info_augmented_with_the_current_snapshot_sequence = () =>
				returnedInfo.ShouldBeLike(new BootstrapInfo(
					providedInfo.JournaledSequence,
					providedInfo.DispatchSequence,
					providedInfo.SerializedTypes,
					providedInfo.DuplicateIdentifiers)
						.AddSnapshotSequence(reader.MessageSequence));

			static SystemSnapshotStreamReader reader;
		}

		public class when_the_latest_journaled_sequence_is_zero
		{
			Establish context = () =>
				providedInfo = new BootstrapInfo(0, 0, new string[7], new Guid[24]);

			Because of = () =>
				returnedInfo = bootstrapper.RestoreSnapshots(providedInfo, repository);

			It should_not_load_any_snapshots = () =>
				snapshots.ReceivedCalls().Count().ShouldEqual(0);

			It should_not_create_a_disruptor = () =>
				disruptors.ReceivedCalls().Count().ShouldEqual(0);

			It should_return_the_existing_bootstrap_info = () =>
				returnedInfo.ShouldEqual(providedInfo);
		}

		public class when_the_latest_snapshot_sequence_is_zero
		{
			Establish context = () =>
			{
				reader = Substitute.For<SystemSnapshotStreamReader>();
				reader.MessageSequence.Returns(0);
				reader.Count.Returns(1); // this should never happen, but we want the test to be sure that we return for the right reason
				snapshots.CreateSystemSnapshotStreamReader(providedInfo.JournaledSequence).Returns(reader);
			};

			Because of = () =>
				returnedInfo = bootstrapper.RestoreSnapshots(providedInfo, repository);

			It should_not_load_any_snapshots = () =>
				reader.Received(0).Read();

			It should_not_create_a_disruptor = () =>
				disruptors.ReceivedCalls().Count().ShouldEqual(0);

			It should_return_the_existing_bootstrap_info = () =>
				returnedInfo.ShouldEqual(providedInfo);

			static SystemSnapshotStreamReader reader;
		}

		Establish context = () =>
		{
			thrown = null;
			snapshots = Substitute.For<SnapshotFactory>();
			disruptors = Substitute.For<DisruptorFactory>();
			repository = Substitute.For<IRepository>();
			providedInfo = new BootstrapInfo(2, 1, new string[0], new Guid[0]);

			bootstrapper = new SnapshotBootstrapper(snapshots, disruptors);
		};
		static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		static SnapshotBootstrapper bootstrapper;
		static SnapshotFactory snapshots;
		static DisruptorFactory disruptors;
		static BootstrapInfo returnedInfo;
		static BootstrapInfo providedInfo;
		static IRepository repository;
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
