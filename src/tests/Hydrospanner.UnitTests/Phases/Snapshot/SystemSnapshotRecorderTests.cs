#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using Machine.Specifications;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_all_parts_of_a_snapshot_have_been_gathered
	{
		It should_flush_the_current_snapshot_to_disk;

		It should_begin_recording_the_next_snapshot; // how to test??
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
