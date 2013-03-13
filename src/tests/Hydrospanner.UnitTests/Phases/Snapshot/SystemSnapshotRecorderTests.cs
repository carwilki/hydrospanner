#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_recording_system_snapshots
	{
		public class and_the_item_is_part_of_a_system_snapshot
		{
			Establish context = () =>
				item.AsPartOfSystemSnapshot(42, 0, "key", "memento");

			It should_pass_the_item_to_the_recorder = () =>
				recorder.Received(1).Record(item);
		}

		public class and_the_item_is_a_public_snapshot
		{
			Establish context = () =>
				item.AsPublicSnapshot("key", "memento");

			It should_NOT_pass_the_item_to_the_record = () =>
				recorder.Received(0).Record(item);
		}

		Establish context = () =>
		{
			recorder = Substitute.For<ISnapshotRecorder>();
			handler = new SystemSnapshotHandler(recorder);
			item = new SnapshotItem();
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		static SystemSnapshotHandler handler;
		static ISnapshotRecorder recorder;
		static SnapshotItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
