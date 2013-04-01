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
				reader = SystemSnapshotStreamReader.Open(0, 1, " \t bogus hash \n ", stream);

			It should_reset_the_stream_position_to_zero = () =>
				stream.Position.ShouldEqual(0);

			It should_return_null = () => 
				reader.ShouldBeNull();
		}

		public class and_the_hash_matches
		{
			Because of = () =>
				reader = SystemSnapshotStreamReader.Open(MessageSequence, 1, correctHash, stream);

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

			var type = Encoding.UTF8.GetBytes(string.Empty.GetType().AssemblyQualifiedName ?? string.Empty);
			foreach (var record in Records)
			{
				var item = record.Value;
				contents.AddRange(BitConverter.GetBytes(type.Length));
				contents.AddRange(type);
				contents.AddRange(BitConverter.GetBytes(item.Length));
				contents.AddRange(item);
			}

			stream = new MemoryStream(contents.ToArray());
			var hasher = new SHA1Managed();
			var hash = new SoapHexBinary(hasher.ComputeHash(contents.ToArray())).ToString();
			reader = SystemSnapshotStreamReader.Open(42, 1, hash, stream);
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
		static readonly List<KeyValuePair<string, byte[]>> Records = new List<KeyValuePair<string, byte[]>>
		{
			new KeyValuePair<string, byte[]>(typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("First")),
			new KeyValuePair<string, byte[]>(typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("Second")),
			new KeyValuePair<string, byte[]>(typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes("Third"))
		};
		static MemoryStream stream;
		static SystemSnapshotStreamReader reader;
		static List<KeyValuePair<string, byte[]>> recordsReadFromSnapshot;
	}

	[Subject(typeof(SystemSnapshotStreamReader))]
	public class when_disposing_a_snapshot
	{
		Establish context = () =>
		{
			stream = new MemoryStream(Contents);
			using (var hasher = new SHA1Managed())
				correctHash = new SoapHexBinary(hasher.ComputeHash(Contents)).ToString();

			reader = SystemSnapshotStreamReader.Open(0, 1, correctHash, stream);
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
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
