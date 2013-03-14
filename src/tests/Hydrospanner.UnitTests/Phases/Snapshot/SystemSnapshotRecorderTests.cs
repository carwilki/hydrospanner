#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using System.Text;
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_recording_snapshots
	{
		public class when_the_first_snapshot_item_is_received
		{
			Because of = () =>
			{
				recorder.Record(first);
				var hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Array)).ToString();
				reader = SnapshotStreamReader.Open(Sequence, 1, hash, new MemoryStream(firstSnapshot.Array));
			};

			It should_open_a_new_snapshot = () =>
				reader.Count.ShouldEqual(3);

			It should_include_that_item_in_the_new_snapshot = () =>
				reader.Read().Single().ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"first\"".ToByteArray()));

			static SnapshotStreamReader reader;
		}

		public class when_a_subsequent_snapshot_item_is_received
		{
			It should_be_included_in_the_snapshot;
		}

		public class when_the_final_snapshot_item_is_received
		{
			It should_include_that_item_in_the_snapshot;
			It should_finalize_the_snapshot;
			It should_fingerprint_the_snapshot_with_an_incremented_iteration_number;
			It should_fingerprint_the_snapshot_with_the_current_message_sequence;
			It should_fingerprint_the_snapshot_with_the_sha1_hash_of_the_file_contents;
		}

		public class when_finalizing_subsequent_snapshots
		{
			It should_continue_to_increment_the_iteration_number;
		}

		Establish context = () =>
		{
			serializer = new JsonSerializer();
			file = Substitute.For<FileBase>();
			recorder = new SystemSnapshotRecorder(file, "/", LatestIteration);
			
			firstSnapshot = new PhotographicMemoryStream();
			subsequentSnapshot = new PhotographicMemoryStream();
			file.Create(Arg.Is<string>(x => x == Location + LatestIteration + "-" + Sequence)).Returns(firstSnapshot);
			file.Create(Arg.Is<string>(x => x == Location + (LatestIteration + 1) + "-" + Sequence)).Returns(subsequentSnapshot);

			first = new SnapshotItem();
			middle = new SnapshotItem();
			last = new SnapshotItem();
			
			first.AsPartOfSystemSnapshot(Sequence, 2, Key, "first");
			middle.AsPartOfSystemSnapshot(Sequence, 1, Key, "middle");
			last.AsPartOfSystemSnapshot(Sequence, 0, Key, "last");

			first.Serialize(serializer);
			middle.Serialize(serializer);
			last.Serialize(serializer);
		};

		const long Sequence = 12345;
		const string Location = "/";
		const int LatestIteration = 39;
		const string Key = "key";
		static FileBase file;
		static SystemSnapshotRecorder recorder;
		static PhotographicMemoryStream firstSnapshot;
		static PhotographicMemoryStream subsequentSnapshot;
		static SnapshotItem first;
		static SnapshotItem middle;
		static SnapshotItem last;
		static JsonSerializer serializer;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
