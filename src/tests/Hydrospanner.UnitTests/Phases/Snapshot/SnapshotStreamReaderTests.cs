#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(SnapshotStreamReader))]
	public class when_opening_a_snapshot
	{
		public class and_the_hash_does_not_match_the_contents_of_the_snapshot
		{
			Because of = () =>
				reader = SnapshotStreamReader.Open(0, 1, " \t bogus hash \n ", stream);

			It should_reset_the_stream_position_to_zero = () =>
				stream.Position.ShouldEqual(0);

			It should_return_null = () => 
				reader.ShouldBeNull();
		}

		public class and_the_hash_matches
		{
			Because of = () =>
				reader = SnapshotStreamReader.Open(MessageSequence, 1, correctHash, stream);

			It should_read_the_first_four_bytes_to_determine_the_number_of_items_in_the_snapshot = () =>
			{
				stream.Position.ShouldEqual(sizeof(int));
				reader.Count.ShouldEqual(42);
			};

			It should_remember_the_sequence_number = () =>
				reader.MessageSequence.ShouldEqual(MessageSequence);
		}

		Establish context = () =>
		{
			stream = new MemoryStream(Contents);
			using (var hasher = new SHA1Managed())
				correctHash = new SoapHexBinary(hasher.ComputeHash(Contents)).ToString().ToLower() + InsignificantOuterWhitespace;
		};

		const long MessageSequence = 123;
		const int NumberOfRecords = 42;
		const string InsignificantOuterWhitespace = " \n \t \r\n ";
		static readonly byte[] Contents = BitConverter.GetBytes(NumberOfRecords);
		static string correctHash;
		static MemoryStream stream;
		static SnapshotStreamReader reader;
	}

	[Subject(typeof(SnapshotStreamReader))]
	public class when_reading_from_a_snapshot
	{
		Establish context = () =>
		{
			var contents = new List<byte>();
			contents.AddRange(BitConverter.GetBytes(Records.Count));

			foreach (var record in Records)
			{
				contents.AddRange(BitConverter.GetBytes(record.Length));
				contents.AddRange(record);
			}

			stream = new MemoryStream(contents.ToArray());
			var hasher = new SHA1Managed();
			var hash = new SoapHexBinary(hasher.ComputeHash(contents.ToArray())).ToString();
			reader = SnapshotStreamReader.Open(42, 1, hash, stream);
		};

		Because of = () =>
			recordsReadFromSnapshot = reader.Read();

		It should_yield_each_record_in_turn = () =>
			recordsReadFromSnapshot.ShouldBeLike(Records);

		const int NumberOfRecords = 42;
		static readonly List<byte[]> Records = new List<byte[]>
		{
			Encoding.UTF8.GetBytes("First"),
			Encoding.UTF8.GetBytes("Second"),
			Encoding.UTF8.GetBytes("Third")
		};
		static MemoryStream stream;
		static SnapshotStreamReader reader;
		static IEnumerable<byte[]> recordsReadFromSnapshot;
	}

	[Subject(typeof(SnapshotStreamReader))]
	public class when_disposing_a_snapshot
	{
		Establish context = () =>
		{
			stream = new MemoryStream(Contents);
			using (var hasher = new SHA1Managed())
				correctHash = new SoapHexBinary(hasher.ComputeHash(Contents)).ToString();

			reader = SnapshotStreamReader.Open(0, 1, correctHash, stream);
		};

		Because of = () =>
			reader.Dispose();

		It should_dispose_the_underlying_stream = () =>
			Catch.Exception(() => stream.ReadByte()).ShouldBeOfType<ObjectDisposedException>();

		static readonly byte[] Contents = BitConverter.GetBytes(42);
		static string correctHash;
		static MemoryStream stream;
		static SnapshotStreamReader reader;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
