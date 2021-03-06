﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_recording_a_system_snapshot
	{
		public class when_starting_a_snapshot_without_having_finished_the_previous_snapshot
		{
			Establish context = () =>
			{
				subsequentSnapshot = new PhotographicMemoryStream();
				file.Create(Arg.Any<string>()).Returns(firstSnapshot, subsequentSnapshot);
				recorder.StartRecording(42);
			};

			Because of = () =>
			{
				recorder.StartRecording(43);
				recorder.FinishRecording();
			};

			It should_close_the_previous_snapshot = () =>
				firstSnapshot.Disposed.ShouldBeTrue();

			It should_start_a_new_snapshot = () =>
				subsequentSnapshot.Contents.SliceInt32(0).ShouldEqual(43);

			It should_delete_any_remnant_on_disk_before_starting_the_new_snapshot = () =>
				file.Received(2).Delete(Location + "current_snapshot");

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
				recorder.StartRecording(42);
				recorder.FinishRecording(); // writes buffered contents to the stream
			};

			It should_delete_any_remnant_on_disk_before_starting_the_new_snapshot = () =>
				file.Received(1).Delete(Location + "current_snapshot");

			It should_open_a_file_for_writing = () =>
				file.Received(1).Create("/current_snapshot");

			It should_write_the_number_of_records_as_the_first_4_bytes = () =>
				firstSnapshot.Contents.SliceInt32(0).ShouldEqual(42);
		}

		public class when_writing_items_to_a_snapshot
		{
			Because of = () =>
			{
				recorder.StartRecording(3);
				recorder.Record(first);
				recorder.Record(middle);
				recorder.Record(last);
				recorder.FinishRecording();
			};

			It should_include_each_item_in_the_list = () =>
			{
				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SystemSnapshotStreamReader.Open(Sequence, hash, new MemoryStream(firstSnapshot.Contents));
				var records = reader.Read().ToList();

				records.Count.ShouldEqual(3);
				records[0].ShouldBeEqual(new Tuple<string, string, byte[]>("k1", string.Empty.ResolvableTypeName(), "\"first\"".ToByteArray()));
				records[1].ShouldBeEqual(new Tuple<string, string, byte[]>("k2", string.Empty.ResolvableTypeName(), "\"middle\"".ToByteArray()));
				records[2].ShouldBeEqual(new Tuple<string, string, byte[]>("k3", string.Empty.ResolvableTypeName(), "\"last\"".ToByteArray()));
			};
		}

		public class when_writing_a_null_memento_to_a_snapshot
		{
			Because of = () =>
			{
				recorder.StartRecording(3);
				recorder.Record(first);
				recorder.Record(new SnapshotItem { Key = "null-key2", MementoType = "some-type", Memento = null });
				recorder.Record(last);
				recorder.FinishRecording();
			};

			It should_include_each_item_in_the_list = () =>
			{
				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SystemSnapshotStreamReader.Open(Sequence, hash, new MemoryStream(firstSnapshot.Contents));
				var records = reader.Read().ToList();

				records.Count.ShouldEqual(3);
				records[0].ShouldBeEqual(new Tuple<string, string, byte[]>("k1", string.Empty.ResolvableTypeName(), "\"first\"".ToByteArray()));
				records[1].ShouldBeEqual(new Tuple<string, string, byte[]>("null-key2", "some-type", null));
				records[2].ShouldBeEqual(new Tuple<string, string, byte[]>("k3", string.Empty.ResolvableTypeName(), "\"last\"".ToByteArray()));
			};
		}

		public class when_the_snapshot_is_finished
		{
			Establish context = () =>
				recorder.StartRecording(3);

			Because of = () =>
			{
				recorder.FinishRecording(Sequence);

				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SystemSnapshotStreamReader.Open(Sequence, hash, new MemoryStream(firstSnapshot.Contents));
			};

			It should_finalize_the_snapshot = () =>
				firstSnapshot.Disposed.ShouldBeTrue();

			It should_name_the_snapshot_using_the_sequence_numbers_and_hash = () =>
				finalPathOfSnapshot.ShouldEqual("{0}{1}-{2}".FormatWith(Location, Sequence, hash));
		}

		public class when_errors_are_raised
		{
			public class when_the_recording_is_started
			{
				Establish context = () =>
					file.Create(Arg.Any<string>()).Returns(x => { throw new DivideByZeroException(); });

				It should_not_allow_them_to_propogate_past_this_recorder = () =>
					Catch.Exception(() => recorder.StartRecording(42)).ShouldBeNull();
			}

			public class when_recording_an_item
			{
				Establish context = () =>
					recorder.StartRecording(42);

				It should_not_allow_them_to_propogate_past_this_recorder = () =>
					Catch.Exception(() => recorder.Record(null)).ShouldBeNull();
			}

			public class when_the_recording_is_finished
			{
				Establish context = () =>
				{
					file.OpenRead(Arg.Any<string>()).Returns(x => { throw new DivideByZeroException(); });
					recorder.StartRecording(42);
				};

				It should_not_allow_them_to_propogate_past_this_recorder = () =>
					Catch.Exception(() => recorder.FinishRecording()).ShouldBeNull();
			}
		}

		Establish context = () =>
		{
			firstSnapshot = new PhotographicMemoryStream();
			serializer = new JsonSerializer();
			directory = Substitute.For<DirectoryBase>();
			file = Substitute.For<FileBase>();
			file.Create(Arg.Any<string>()).Returns(firstSnapshot);
			file.OpenRead(Arg.Any<string>()).Returns(x => new MemoryStream(firstSnapshot.Contents));
			file.Move(Arg.Any<string>(), Arg.Do<string>(x => finalPathOfSnapshot = x));
			recorder = new SystemSnapshotRecorder(directory, file, Location);
			
			first = new SnapshotItem();
			middle = new SnapshotItem();
			last = new SnapshotItem();

			first.AsPartOfSystemSnapshot(Sequence, 2, "k1", "first", typeof(string));
			middle.AsPartOfSystemSnapshot(Sequence, 1, "k2", "middle", typeof(string));
			last.AsPartOfSystemSnapshot(Sequence, 0, "k3", "last", typeof(string));

			first.Serialize(serializer);
			middle.Serialize(serializer);
			last.Serialize(serializer);
		};

		const long Sequence = 12345;
		const string Location = "/";
		const string Key = "key";
		static string hash;
		static string finalPathOfSnapshot;
		static DirectoryBase directory;
		static FileBase file;
		static SystemSnapshotRecorder recorder;
		static PhotographicMemoryStream firstSnapshot;
		static SnapshotItem first;
		static SnapshotItem middle;
		static SnapshotItem last;
		static JsonSerializer serializer;
		static SystemSnapshotStreamReader reader;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
