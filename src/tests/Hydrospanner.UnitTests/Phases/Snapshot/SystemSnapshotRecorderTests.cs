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
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_recording_snapshots
	{
		public class when_starting_a_snapshot_without_having_finished_the_previous_snapshot
		{
			Establish context = () =>
			{
				subsequentSnapshot = new PhotographicMemoryStream();
				file.Create(Arg.Is<string>(x => x == Location + (LatestIteration + 1) + "-" + (Sequence + 1))).Returns(subsequentSnapshot);
				recorder.StartRecording(Sequence, LatestIteration, 42);
			};

			Because of = () =>
			{
				recorder.StartRecording(Sequence + 1, LatestIteration + 1, 43);
				recorder.FinishRecording();
			};

			It should_close_the_previous_snapshot = () =>
				firstSnapshot.Disposed.ShouldBeTrue();

			It should_start_a_new_snapshot = () =>
				subsequentSnapshot.Contents.SliceInt32(0).ShouldEqual(43);

			static PhotographicMemoryStream subsequentSnapshot;
		}

		public class when_attempting_to_record_an_item_without_having_started_the_recording_process
		{
			It should_not_record_the_item = () =>
				Catch.Exception(() => recorder.Record(first)).ShouldBeNull();
		}

		public class when_attempting_to_finish_a_snapshot_that_was_never_started
		{
			It should_do_nothing = () =>
				Catch.Exception(() => recorder.FinishRecording()).ShouldBeNull();
		}

		public class when_starting_a_snapshot
		{
			Because of = () =>
			{
				recorder.StartRecording(Sequence, LatestIteration, 42);
				recorder.FinishRecording(); // writes buffered contents to the stream
			};

			It should_open_a_file_for_writing = () =>
				file.Received(1).Create(ExpectedInitialFilename);

			It should_write_the_number_of_records_as_the_first_4_bytes = () =>
				firstSnapshot.Contents.SliceInt32(0).ShouldEqual(42);

			static readonly string ExpectedInitialFilename = "/" + LatestIteration + "-" + Sequence;
		}

		public class when_writing_items_to_a_snapshot
		{
			Because of = () =>
			{
				recorder.StartRecording(Sequence, LatestIteration, 3);
				recorder.Record(first);
				recorder.Record(middle);
				recorder.Record(last);
				recorder.FinishRecording();
			};

			It should_include_each_item_in_the_list = () =>
			{
				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SnapshotStreamReader.Open(Sequence, 1, hash, new MemoryStream(firstSnapshot.Contents));
				var records = reader.Read().ToList();
				
				records.Count().ShouldEqual(3);
				records.First().ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"first\"".ToByteArray()));
				records.ElementAt(1).ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"middle\"".ToByteArray()));
				records.Last().ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"last\"".ToByteArray()));
			};
		}

		public class when_the_snapshot_is_finished
		{
			Establish context = () =>
				recorder.StartRecording(Sequence, LatestIteration, 3);

			Because of = () =>
			{
				recorder.FinishRecording();

				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SnapshotStreamReader.Open(Sequence, 1, hash, new MemoryStream(firstSnapshot.Contents));
			};

			It should_finalize_the_snapshot = () =>
				firstSnapshot.Disposed.ShouldBeTrue();

			It should_name_the_snapshot_using_the_iteration_and_sequence_numbers_and_hash = () =>
				finalPathOfSnapshot.ShouldEqual("{0}{1}-{2}-{3}".FormatWith(Location, LatestIteration, Sequence, hash));
		}

		Establish context = () =>
		{
			firstSnapshot = new PhotographicMemoryStream();
			serializer = new JsonSerializer();
			file = Substitute.For<FileBase>();
			file.Create(Arg.Is<string>(x => x == Location + LatestIteration + "-" + Sequence)).Returns(firstSnapshot);
			file.OpenRead(Arg.Any<string>()).Returns(x => new MemoryStream(firstSnapshot.Contents));
			file.Move(Arg.Any<string>(), Arg.Do<string>(x => finalPathOfSnapshot = x));
			recorder = new SystemSnapshotRecorder(file, Location);
			
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
		static string hash;
		static string finalPathOfSnapshot;
		static FileBase file;
		static SystemSnapshotRecorder recorder;
		static PhotographicMemoryStream firstSnapshot;
		static SnapshotItem first;
		static SnapshotItem middle;
		static SnapshotItem last;
		static JsonSerializer serializer;
		static SnapshotStreamReader reader;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
