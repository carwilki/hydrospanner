#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using System.Text;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(SystemSnapshotLoader))]
	public class when_loading_snapshots
	{
		public class and_there_are_no_completed_snapshots_to_load
		{
			Establish context = () =>
				directory
					.GetFiles(Path, Arg.Any<string>(), SearchOption.TopDirectoryOnly)
					.Returns(new[] { "hi", "not-a-snapshot", "blah-blah-blah" });

			Because of = () =>
				reader = loader.Load(long.MaxValue);

			It should_return_a_blank_snapshot = () =>
				reader.Count.ShouldEqual(0);
		}

		public class and_there_are_no_viable_snapshots
		{
			Establish context = () =>
			{
				file.OpenRead(Arg.Any<string>()).Returns(new MemoryStream(new byte[] { 0, 0, 0, 0 }));
				directory
					.GetFiles(Path, Arg.Any<string>(), SearchOption.TopDirectoryOnly)
					.Returns(new[] { Path + MessageSequence + "-bad_hash" });
			};

			Because of = () =>
				reader = loader.Load(long.MaxValue);

			It should_return_a_blank_snapshot = () =>
				reader.Count.ShouldEqual(0);
		}

		public class when_loading_a_snapshot_based_on_message_sequence
		{
			Establish context = () =>
			{
				file = Substitute.For<FileBase>();
				directory = Substitute.For<DirectoryBase>();
				loader = new SystemSnapshotLoader(directory, file, Path);

				var reallyEarlyPath = Path + ReallyEarlySnapshotSequence + "-" + hash;
				var earlierPath = Path + EarlySnapshotSequence + "-" + hash;
				var laterPath = Path + LaterSnapshotSequence + "-" + hash;

				directory
					.GetFiles(Path, "*", SearchOption.TopDirectoryOnly)
					.Returns(new[] { laterPath, reallyEarlyPath, earlierPath });

				file.OpenRead(reallyEarlyPath).Returns(new MemoryStream(contents));
				file.OpenRead(earlierPath).Returns(new MemoryStream(contents));
				file.OpenRead(laterPath).Returns(new MemoryStream(contents));
			};

			Because of = () =>
				reader = loader.Load(StoredMessageSequence);

			It should_load_the_snapshot_whose_message_sequence_is_closest_to_but_higher_than_the_provided_sequence = () =>
			{
				reader.MessageSequence.ShouldEqual(EarlySnapshotSequence);
				reader.Read().First().Value.ShouldBeLike(FirstRecord);
			};

			const int StoredMessageSequence = 42;
			const long ReallyEarlySnapshotSequence = StoredMessageSequence - 1;
			const long EarlySnapshotSequence = StoredMessageSequence;
			const long LaterSnapshotSequence = StoredMessageSequence + 1;
		}

		Establish context = () =>
		{
			file = Substitute.For<FileBase>();
			directory = Substitute.For<DirectoryBase>();
			loader = new SystemSnapshotLoader(directory, file, Path);

			var oneRecord = BitConverter.GetBytes(1);
			var firstRecordType = Encoding.UTF8.GetBytes(FirstRecord.GetType().AssemblyQualifiedName ?? string.Empty);
			var firstRecordTypeLength = BitConverter.GetBytes(firstRecordType.Length);
			var firstRecordLength = BitConverter.GetBytes(4);
			contents = oneRecord
				.Concat(firstRecordTypeLength)
				.Concat(firstRecordType)
				.Concat(firstRecordLength)
				.Concat(FirstRecord).ToArray();

			hash = new SoapHexBinary(new SHA1Managed().ComputeHash(contents)).ToString();
		};

		const string Path = "./path/to/snapshots/";
		const int MessageSequence = 42;
		static readonly byte[] FirstRecord = BitConverter.GetBytes(42);
		static SystemSnapshotLoader loader;
		static SystemSnapshotStreamReader reader;
		static DirectoryBase directory;
		static FileBase file;
		static byte[] contents;
		static string hash;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
