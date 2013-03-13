#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using Machine.Specifications;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_the_first_snapshot_item_is_received
	{
		It should_open_a_new_snapshot;
		It should_include_that_item_in_the_new_snapshot;
	}

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_a_subsequent_snapshot_item_is_received
	{
		It should_be_included_in_the_snapshot;
	}

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_the_final_snapshot_item_is_received
	{
		It should_include_that_item_in_the_snapshot;
		It should_finalize_the_snapshot;
		It should_fingerprint_the_snapshot_with_an_incremented_iteration_number;
		It should_fingerprint_the_snapshot_with_the_current_message_sequence;
		It should_fingerprint_the_snapshot_with_the_sha1_hash_of_the_file_contents;
	}

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_finalizing_subsequent_snapshots
	{
		It should_continue_to_increment_the_iteration_number;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
