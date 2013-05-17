#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(SystemSnapshotStreamReader))]
	public class when_opening_a_snapshot
	{
		public class and_the_hash_does_not_match_the_contents_of_the_snapshot
		{
			Because of = () =>
				reader = SystemSnapshotStreamReader.Open(0, " \t bogus hash \n ", stream);

			It should_reset_the_stream_position_to_zero = () =>
				stream.Position.ShouldEqual(0);

			It should_return_null = () => 
				reader.ShouldBeNull();
		}

		public class and_the_hash_matches
		{
			Because of = () =>
				reader = SystemSnapshotStreamReader.Open(MessageSequence, correctHash, stream);

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
		static SystemSnapshotStreamReader reader;
	}

	[Subject(typeof(SystemSnapshotStreamReader))]
	public class when_reading_from_a_snapshot
	{
		Establish context = () =>
		{
			var contents = new List<byte>();
			contents.AddRange(BitConverter.GetBytes(Records.Count));

			foreach (var record in Records)
			{
				var keyBytes = Encoding.UTF8.GetBytes(record.Item1);
				var typeBytes = Encoding.UTF8.GetBytes(record.Item2);
				contents.AddRange(BitConverter.GetBytes(keyBytes.Length));
				contents.AddRange(keyBytes);
				contents.AddRange(BitConverter.GetBytes(typeBytes.Length));
				contents.AddRange(typeBytes);
				contents.AddRange(BitConverter.GetBytes(record.Item3.Length));
				contents.AddRange(record.Item3);
			}

			stream = new MemoryStream(contents.ToArray());
			var hasher = new SHA1Managed();
			var hash = new SoapHexBinary(hasher.ComputeHash(contents.ToArray())).ToString();
			reader = SystemSnapshotStreamReader.Open(42, hash, stream);
		};

		Because of = () =>
			recordsReadFromSnapshot = reader.Read().ToList();

		It should_yield_each_record_in_turn = () =>
		{
			recordsReadFromSnapshot[0].ShouldBeEqual(Records[0]);
			recordsReadFromSnapshot[1].ShouldBeEqual(Records[1]);
			recordsReadFromSnapshot[2].ShouldBeEqual(Records[2]);
		};

		const int NumberOfRecords = 42;
		static readonly List<Tuple<string, string, byte[]>> Records = new List<Tuple<string, string, byte[]>>
		{
			new Tuple<string, string, byte[]>(string.Empty, typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("First")),
			new Tuple<string, string, byte[]>(string.Empty, typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("Second")),
			new Tuple<string, string, byte[]>(string.Empty, typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("Third"))
		};
		static MemoryStream stream;
		static SystemSnapshotStreamReader reader;
		static List<Tuple<string, string, byte[]>> recordsReadFromSnapshot;
	}

	[Subject(typeof(SystemSnapshotStreamReader))]
	public class when_disposing_a_snapshot
	{
		Establish context = () =>
		{
			stream = new MemoryStream(Contents);
			using (var hasher = new SHA1Managed())
				correctHash = new SoapHexBinary(hasher.ComputeHash(Contents)).ToString();

			reader = SystemSnapshotStreamReader.Open(0, correctHash, stream);
		};

		Because of = () =>
			reader.Dispose();

		It should_dispose_the_underlying_stream = () =>
			Catch.Exception(() => stream.ReadByte()).ShouldBeOfType<ObjectDisposedException>();

		static readonly byte[] Contents = BitConverter.GetBytes(42);
		static string correctHash;
		static MemoryStream stream;
		static SystemSnapshotStreamReader reader;
	}

	[Subject(typeof(SystemSnapshotStreamReader))]
	public class when_disposing_a_blank_snapshot
	{
		It should_NOT_throw = () =>
			Catch.Exception(() => new SystemSnapshotStreamReader().Dispose()).ShouldBeNull();
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
