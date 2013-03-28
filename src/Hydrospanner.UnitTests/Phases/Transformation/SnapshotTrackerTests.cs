#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using Machine.Specifications;
	using NSubstitute;
	using Snapshot;

	[Subject(typeof(SnapshotTracker))]
	public class when_tracking_system_snapshots
	{
		public class when_constructor_parameters_are_invalid
		{
			It should_throw_if_the_journaled_sequence_is_out_of_range = () =>
			{
				Try(() => new SnapshotTracker(-1, 100, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SnapshotTracker(long.MinValue, 100, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_frequency_is_out_of_range = () =>
			{
				Try(() => new SnapshotTracker(1, 99, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SnapshotTracker(1, 0, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SnapshotTracker(1, int.MinValue, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_snapshot_ring_is_null = () =>
				Try(() => new SnapshotTracker(1, 100, null, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_repository_is_null = () =>
				Try(() => new SnapshotTracker(1, 100, snapshots, null)).ShouldBeOfType<ArgumentNullException>();

			static Exception Try(Action action)
			{
				return Catch.Exception(action);
			}
		}

		public class when_the_snapshot_tracker_is_not_yet_ready_for_a_snapshot
		{
			Establish context = () =>
				tracker = new SnapshotTracker(98, 100, snapshots, repository);

			Because of = () =>
				tracker.Increment(1);

			It should_NOT_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeEmpty();
		}

		public class when_the_snapshot_tracker_lands_on_the_snapshot_frequency
		{
			Establish context = () =>
				tracker = new SnapshotTracker(99, 100, snapshots, repository);

			Because of = () =>
				tracker.Increment(1);

			It should_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 100,
						Memento = 2,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 100,
						Memento = 1,
						MementosRemaining = 0
					}
				});
		}

		public class when_the_snapshot_tracker_goes_beyond_the_snapshot_frequency
		{
			Establish context = () =>
				tracker = new SnapshotTracker(99, 100, snapshots, repository);

			Because of = () =>
				tracker.Increment(2);

			It should_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 2,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 1,
						MementosRemaining = 0
					}
				});
		}

		public class when_the_snapshot_tracker_does_NOT_go_beyond_the_snapshot_frequency_on_a_subsequent_increment
		{
			Establish context = () =>
			{
				tracker = new SnapshotTracker(99, 100, snapshots, repository);
				tracker.Increment(100); // will cross here
			};

			Because of = () =>
				tracker.Increment(2); // will cross here too
		}

		public class when_the_snapshot_tracker_goes_beyond_the_snapshot_frequency_on_a_subsequent_increment
		{
			Establish context = () =>
			{
				tracker = new SnapshotTracker(99, 100, snapshots, repository);
				tracker.Increment(2); // will cross here
			};

			Because of = () =>
				tracker.Increment(2); // will NOT cross here

			It should_generate_a_snapshot_second_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 2,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 1,
						MementosRemaining = 0
					}
				});
		}

		Establish context = () =>
		{
			snapshots = new RingBufferHarness<SnapshotItem>();
			repository = Substitute.For<IRepository>();
			repository.GetMementos().Returns(new object[] { 1, 2 });
		};

		static SnapshotTracker tracker;
		static RingBufferHarness<SnapshotItem> snapshots;
		static IRepository repository;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
