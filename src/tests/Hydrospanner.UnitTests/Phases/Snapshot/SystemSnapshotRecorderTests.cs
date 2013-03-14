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
		public class when_all_items_for_a_snapshot_have_been_received
		{
			Establish context = () =>
			{
				file.Move(Arg.Any<string>(), Arg.Do<string>(x => finalPathOfSnapshot = x));
				file.OpenRead(Arg.Any<string>()).Returns(x => new MemoryStream(firstSnapshot.Contents));
				recorder.Record(first);
				recorder.Record(middle);
			};

			Because of = () =>
			{
				recorder.Record(last);
				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(firstSnapshot.Contents)).ToString();
				reader = SnapshotStreamReader.Open(Sequence, 1, hash, new MemoryStream(firstSnapshot.Contents));
			};

			It should_include_each_item_in_the_list = () =>
			{
				var records = reader.Read().ToList();
				records.Count().ShouldEqual(3);
				records.First().ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"first\"".ToByteArray()));
				records.ElementAt(1).ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"middle\"".ToByteArray()));
				records.Last().ShouldBeEqual(new KeyValuePair<Type, byte[]>(typeof(string), "\"last\"".ToByteArray()));
			};

			It should_finalize_the_snapshot = () =>
				firstSnapshot.Disposed.ShouldBeTrue();

			It should_name_the_snapshot_using_the_iteration_and_sequence_numbers_and_hash = () =>
				finalPathOfSnapshot.ShouldEqual("{0}{1}-{2}-{3}".FormatWith(Location, LatestIteration, Sequence, hash));
		}

		public class when_finalizing_subsequent_snapshots
		{
			Establish context = () =>
			{
				file.Move(Arg.Any<string>(), Arg.Do<string>(x => finalPathOfSnapshot = x));
				file.OpenRead(Arg.Any<string>())
					.Returns(
						x => new MemoryStream(firstSnapshot.Contents), 
						x => new MemoryStream(subsequentSnapshot.Contents));
				
				recorder.Record(first);
				recorder.Record(middle);
				recorder.Record(last);

				first.AsPartOfSystemSnapshot(Sequence + 1, 0, "key", "memento");
				first.Serialize(serializer);
			};

			Because of = () =>
			{
				recorder.Record(first);
				hash = new SoapHexBinary(new SHA1Managed().ComputeHash(subsequentSnapshot.Contents)).ToString();
			};

			It should_finalize_the_subsequent_snapshot = () =>
				subsequentSnapshot.Disposed.ShouldBeTrue();

			It should_continue_to_increment_the_iteration_number_and_stamp_the_filename = () =>
				finalPathOfSnapshot.ShouldEqual("{0}{1}-{2}-{3}".FormatWith(Location, LatestIteration + 1, Sequence + 1, hash));
		}

		Establish context = () =>
		{
			serializer = new JsonSerializer();
			file = Substitute.For<FileBase>();
			recorder = new SystemSnapshotRecorder(file, "/", LatestIteration);
			
			firstSnapshot = new PhotographicMemoryStream();
			subsequentSnapshot = new PhotographicMemoryStream();
			file.Create(Arg.Is<string>(x => x == Location + LatestIteration + "-" + Sequence)).Returns(firstSnapshot);
			file.Create(Arg.Is<string>(x => x == Location + (LatestIteration + 1) + "-" + (Sequence + 1))).Returns(subsequentSnapshot);

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
		static PhotographicMemoryStream subsequentSnapshot;
		static SnapshotItem first;
		static SnapshotItem middle;
		static SnapshotItem last;
		static JsonSerializer serializer;
		static SnapshotStreamReader reader;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
