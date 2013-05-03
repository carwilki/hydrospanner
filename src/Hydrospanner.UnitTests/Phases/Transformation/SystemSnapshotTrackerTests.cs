#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using NSubstitute;
	using Snapshot;

	[Subject(typeof(SystemSnapshotTracker))]
	public class when_tracking_system_snapshots
	{
		public class when_constructor_parameters_are_invalid
		{
			It should_throw_if_the_journaled_sequence_is_out_of_range = () =>
			{
				Try(() => new SystemSnapshotTracker(-1, 100, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SystemSnapshotTracker(long.MinValue, 100, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_frequency_is_out_of_range = () =>
			{
				Try(() => new SystemSnapshotTracker(1, 99, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SystemSnapshotTracker(1, 0, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new SystemSnapshotTracker(1, int.MinValue, snapshots, repository)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_snapshot_ring_is_null = () =>
				Try(() => new SystemSnapshotTracker(1, 100, null, repository)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_repository_is_null = () =>
				Try(() => new SystemSnapshotTracker(1, 100, snapshots, null)).ShouldBeOfType<ArgumentNullException>();

			static Exception Try(Action action)
			{
				return Catch.Exception(action);
			}
		}

		public class when_the_snapshot_tracker_is_not_yet_ready_for_a_snapshot
		{
			Establish context = () =>
				tracker = new SystemSnapshotTracker(98, 100, snapshots, repository);

			Because of = () =>
				tracker.Track(99);

			It should_NOT_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeEmpty();
		}

		public class when_the_snapshot_tracker_lands_on_the_snapshot_frequency
		{
			Establish context = () =>
				tracker = new SystemSnapshotTracker(99, 100, snapshots, repository);

			Because of = () =>
				tracker.Track(100);

			It should_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 100,
						Memento = 1,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 100,
						Memento = 2,
						MementosRemaining = 0
					}
				});
		}

		public class when_the_snapshot_tracker_goes_beyond_the_snapshot_frequency
		{
			Establish context = () =>
				tracker = new SystemSnapshotTracker(99, 100, snapshots, repository);

			Because of = () =>
				tracker.Track(101);

			It should_generate_a_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 1,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 2,
						MementosRemaining = 0
					}
				});
		}

		public class when_the_snapshot_tracker_goes_beyond_the_snapshot_frequency_on_a_subsequent_increment
		{
			Establish context = () =>
			{
				tracker = new SystemSnapshotTracker(99, 100, snapshots, repository);
				tracker.Track(101); // will cross here
			};

			Because of = () =>
				tracker.Track(201); // will cross here too
		}

		public class when_the_snapshot_tracker_does_NOT_go_beyond_the_snapshot_frequency_on_a_subsequent_increment
		{
			Establish context = () =>
			{
				tracker = new SystemSnapshotTracker(99, 100, snapshots, repository);
				tracker.Track(101); // will cross here
			};

			Because of = () =>
				tracker.Track(102); // will NOT cross here

			It should_generate_a_snapshot_second_snapshot = () =>
				snapshots.AllItems.ShouldBeLike(new[] 
				{ 
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 1,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 101,
						Memento = 2,
						MementosRemaining = 0
					},
				});
		}

		public class when_enough_messages_are_tracked_that_one_or_more_snapshots_are_skipped
		{
			Establish context = () =>
			{
				tracker = new SystemSnapshotTracker(1, 100, snapshots, repository);
				tracker.Track(200); // skips several snapshots; should generate a snapshot
			};

			Because of = () =>
			{
				tracker.Track(201); // should not generate another snapshot
				tracker.Track(300); // should generate another snapshot
			};

			It should_continue_tracking_the_snapshot_from_the_provided_current_sequence = () =>
				snapshots.AllItems.ShouldBeLike(new[]
				{
					// First snapshot @ 200
					new SnapshotItem
					{
						CurrentSequence = 200,
						Memento = 1,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 200,
						Memento = 2,
						MementosRemaining = 0
					},

					// Second snapshot @ 300
					new SnapshotItem
					{
						CurrentSequence = 300,
						Memento = 1,
						MementosRemaining = 1
					},
					new SnapshotItem
					{
						CurrentSequence = 300,
						Memento = 2,
						MementosRemaining = 0
					}
				});
		}

		Establish context = () =>
		{
			snapshots = new RingBufferHarness<SnapshotItem>();
			repository = Substitute.For<IRepository>();

			var hydro1 = Substitute.For<IHydratable>();
			hydro1.Memento.Returns(1);

			var hydro2 = Substitute.For<IHydratable>();
			hydro2.Memento.Returns(2);
			var list = new List<IHydratable>(new[] { hydro1, hydro2 });

			repository.Items.Returns(list);
		};

		static SystemSnapshotTracker tracker;
		static RingBufferHarness<SnapshotItem> snapshots;
		static IRepository repository;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
