#pragma warning disable 169
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
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(publicSnapshot, 0, false);
			};

			It should_not_record_any_public_snapshots = () =>
				recorder.Received(0).Record(publicSnapshot);
		}

		public class at_end_of_batch
		{
			Because of = () =>
			{
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(systemSnapshot, 1, true);
			};

			It should_record_the_snapshot = () =>
				Received.InOrder(() =>
				{
					recorder.StartRecording(publicSnapshot.MementosRemaining + 1);
					recorder.Record(publicSnapshot);
					recorder.FinishRecording();
				});

			It should_ignore_the_system_snapshot = () =>
				recorder.Received(0).Record(systemSnapshot);
		}

		public class at_the_end_of_batch_with_not_public_snapshot_items_having_arrived
		{
			Establish context = () =>
			{
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
				handler.OnNext(systemSnapshot, 1, false);
			};

			Because of = () =>
				handler.OnNext(systemSnapshot, 1, true);

			It should_not_attempt_any_record_actions = () =>
			{
				recorder.Received(0).StartRecording(Arg.Any<int>());
				recorder.Received(0).Record(Arg.Any<SnapshotItem>());
				recorder.Received(0).FinishRecording();
			};
		}

		public class at_end_of_subsequent_batch
		{
			Establish context = () =>
			{
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(publicSnapshot, 0, true);
			};

			Because of = () =>
			{
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(publicSnapshot, 0, false);
				handler.OnNext(publicSnapshot, 0, true);
			};

			It should_have_cleared_the_buffer_to_allow_recording_of_the_next_batch = () =>
				recorder.Received(EachItemOnlyOnce).Record(publicSnapshot);
			
			const int EachItemOnlyOnce = 5;
		}

		Establish context = () =>
		{
			publicSnapshot = new SnapshotItem();
			systemSnapshot = new SnapshotItem();
			publicSnapshot.AsPublicSnapshot("public_key", "public_memento");
			systemSnapshot.AsPartOfSystemSnapshot(42, 42, "system_key", "system_memento");
			recorder = Substitute.For<ISnapshotRecorder>();
			handler = new PublicSnapshotHandler(recorder);
		};
				static PublicSnapshotHandler handler;
		static ISnapshotRecorder recorder;
		static SnapshotItem publicSnapshot;
		static SnapshotItem systemSnapshot;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
