#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using Machine.Specifications;
	using NSubstitute;
	using NSubstitute.Experimental;

	[Subject(typeof(PublicSnapshotHandler))]
	public class when_recording_a_public_snapshot
	{
		public class before_the_end_of_batch
		{
			Because of = () =>
			{
				handler.OnNext(publicSnapshot1, 0, false);
				handler.OnNext(publicSnapshot2, 0, false);
				handler.OnNext(publicSnapshot3, 0, false);
			};

			It should_not_record_any_public_snapshots = () =>
				recorder.Received(0).Record(publicSnapshot1);
		}

		public class at_end_of_batch
		{
			Because of = () =>
			{
				handler.OnNext(publicSnapshot1, 0, false);
				handler.OnNext(systemSnapshot, 1, EndOfBatch);
			};

			It should_record_the_snapshot = () =>
				Received.InOrder(() =>
				{
					recorder.StartRecording(publicSnapshot1.MementosRemaining + 1);
					recorder.Record(publicSnapshot1);
					recorder.FinishRecording();
				});

			It should_ignore_the_system_snapshot = () =>
				recorder.Received(0).Record(systemSnapshot);
		}

		public class at_the_end_of_batch_with_no_public_snapshot_items_having_arrived
		{
			Establish context = () =>
			{
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
			};

			Because of = () =>
				handler.OnNext(systemSnapshot, 1, EndOfBatch);

			It should_not_attempt_any_record_actions = () =>
			{
				recorder.Received(0).StartRecording(Arg.Any<int>());
				recorder.Received(0).Record(Arg.Any<SnapshotItem>());
				recorder.Received(0).FinishRecording();
			};
		}

		public class when_public_snapshots_for_the_same_key_arrive
		{
			Because of = () =>
			{
				handler.OnNext(publicSnapshot1, 0, false);
				handler.OnNext(publicSnapshot2, 0, EndOfBatch);
			};

			It should_discard_the_older_snapshot = () =>
				recorder.Received(0).Record(publicSnapshot1);

			It should_use_the_most_recent_for_the_snapshot_recording = () =>
				recorder.Received(1).Record(publicSnapshot2);
		}

		public class at_end_of_a_subsequent_batch
		{
			Establish context = () =>
			{
				handler.OnNext(publicSnapshot2, 0, false);
				handler.OnNext(publicSnapshot3, 0, EndOfBatch);
			};

			Because of = () =>
			{
				handler.OnNext(publicSnapshot2, 0, false);
				handler.OnNext(publicSnapshot3, 0, EndOfBatch);
			};

			It should_have_cleared_the_buffer_to_allow_recording_of_the_next_batch = () =>
			{
				recorder.Received(EachItemTwice).Record(publicSnapshot2);
				recorder.Received(EachItemTwice).Record(publicSnapshot3);
			};
			
			const int EachItemTwice = 2;
		}

		Establish context = () =>
		{
			publicSnapshot1 = new SnapshotItem();
			publicSnapshot2 = new SnapshotItem();
			publicSnapshot3 = new SnapshotItem();
			systemSnapshot = new SnapshotItem();
			publicSnapshot1.AsPublicSnapshot("shared_public_key", "public_memento", typeof(string), 42);
			publicSnapshot2.AsPublicSnapshot("shared_public_key", "public_memento", typeof(string), 42);
			publicSnapshot3.AsPublicSnapshot("non_shared_public_key", "public_memento", typeof(string), 42);
			systemSnapshot.AsPartOfSystemSnapshot(42, 42, "system_memento", typeof(string));
			recorder = Substitute.For<ISnapshotRecorder>();
			handler = new PublicSnapshotHandler(recorder);
		};

		const bool EndOfBatch = true;
		static PublicSnapshotHandler handler;
		static ISnapshotRecorder recorder;
		static SnapshotItem publicSnapshot1;
		static SnapshotItem publicSnapshot2;
		static SnapshotItem publicSnapshot3;
		static SnapshotItem systemSnapshot;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
