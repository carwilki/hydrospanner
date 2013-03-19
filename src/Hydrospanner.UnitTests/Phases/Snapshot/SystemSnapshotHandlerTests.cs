#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SystemSnapshotHandler))]
	public class when_recording_system_snapshots
	{
		public class and_the_item_is_a_public_snapshot
		{
			Establish context = () =>
				first.AsPublicSnapshot("key", "memento", 42);

			It should_not_do_any_recording_operations = () =>
			{
				recorder.Received(0).StartRecording(Arg.Any<int>());
				recorder.Received(0).Record(Arg.Any<SnapshotItem>());
				recorder.Received(0).FinishRecording(Arg.Any<int>(), Arg.Any<long>());
			};
		}

		public class and_the_item_is_part_of_a_system_snapshot
		{
			public class and_the_item_is_the_first_item_in_the_snapshot
			{
				Because of = () =>
					handler.OnNext(first, 0, false);

				It should_start_recording_a_new_snapshot = () =>
					recorder.Received(1).StartRecording(3);

				It should_record_the_item = () =>
					recorder.Received(1).Record(first);
			}

			public class and_the_item_is_in_the_middle_of_the_snapshot
			{
				Establish context = () =>
					handler.OnNext(first, 0, false);
				
				Because of = () =>
					handler.OnNext(middle, 0, false);

				It should_not_start_a_new_snapshot = () =>
					recorder.Received(1).StartRecording(Arg.Any<int>());

				It should_record_the_item = () =>
					recorder.Received(1).Record(middle);
			}

			public class and_the_item_is_the_last_item_in_the_snapshot
			{
				Establish context = () =>
				{
					handler.OnNext(first, 0, false);
					handler.OnNext(middle, 0, false);
				};

				Because of = () =>
					handler.OnNext(last, 0, false);

				It should_not_start_a_new_snapshot = () =>
					recorder.Received(1).StartRecording(Arg.Any<int>());
					
				It should_record_the_item = () =>
					recorder.Received(1).Record(last);

				It should_finish_the_snapshot = () =>
					recorder.Received(1).FinishRecording(CurrentIteration, last.CurrentSequence);
			}

			public class and_the_item_is_for_a_subsequent_snapshot
			{
				Establish context = () =>
				{
					firstOfNextSnapshot = new SnapshotItem();
					firstOfNextSnapshot.AsPartOfSystemSnapshot(42, ItemsInNextSnapshot - 1, "newKey", "newMemento");
					firstOfNextSnapshot.Serialize(new JsonSerializer());

					handler.OnNext(first, 0, false);
					handler.OnNext(middle, 0, false);
					handler.OnNext(last, 0, false);
				};

				Because of = () =>
					handler.OnNext(firstOfNextSnapshot, 0, false);

				It should_start_a_new_snapshot = () =>
					recorder.Received(1).StartRecording(ItemsInNextSnapshot);

				It should_record_the_item = () =>
					recorder.Received(1).Record(firstOfNextSnapshot);

				const int ItemsInNextSnapshot = 10;
				static SnapshotItem firstOfNextSnapshot;
			}
		}

		Establish context = () =>
		{
			recorder = Substitute.For<ISnapshotRecorder>();
			handler = new SystemSnapshotHandler(recorder, CurrentIteration);
			
			first = new SnapshotItem();
			middle = new SnapshotItem();
			last = new SnapshotItem();

			first.AsPartOfSystemSnapshot(0, 2, "key", "first");
			middle.AsPartOfSystemSnapshot(0, 1, "key", "middle");
			last.AsPartOfSystemSnapshot(0, 0, "key", "last");

			var serializer = new JsonSerializer();
			first.Serialize(serializer);
			middle.Serialize(serializer);
			last.Serialize(serializer);
		};

		const int CurrentIteration = 42;
		static SystemSnapshotHandler handler;
		static ISnapshotRecorder recorder;
		static SnapshotItem first;
		static SnapshotItem middle;
		static SnapshotItem last;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
