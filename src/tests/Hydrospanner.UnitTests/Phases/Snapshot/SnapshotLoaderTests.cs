#pragma warning disable 169
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

	[Subject(typeof(SnapshotLoader))]
	public class when_loading_snapshots
	{
		public class and_there_are_no_completed_snapshots_to_load
		{
			Establish context = () =>
				directory
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(EarlierIteration)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { "hi", "not-a-snapshot", "blah-blah-blah", "bad_iteration-42-hash" });

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
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(EarlierIteration)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { Path + EarlierIteration + "-" + MessageSequence + "-bad_hash" });
			};

			Because of = () =>
				reader = loader.Load(long.MaxValue);

			It should_return_a_blank_snapshot = () =>
				reader.Count.ShouldEqual(0);
		}

		public class and_there_is_at_least_one_viable_snapshot
		{
			Establish context = () =>
			{
				var oneRecord = BitConverter.GetBytes(1);
				var firstRecordLength = BitConverter.GetBytes(4);
				var contents = oneRecord.Concat(firstRecordLength).Concat(FirstRecord).ToArray();
				var hash = new SoapHexBinary(new SHA1Managed().ComputeHash(contents));
				var earlierPath = Path + EarlierIteration + "-" + MessageSequence + "-" + hash;
				var laterPath = Path + LaterIteration + "-" + MessageSequence + "-" + hash;

				directory
					.GetFiles(Path, "*", SearchOption.TopDirectoryOnly)
					.Returns(new[] { earlierPath, laterPath });

				file.OpenRead(earlierPath).Returns(new MemoryStream(contents));
				file.OpenRead(laterPath).Returns(new MemoryStream(contents));
			};

			Because of = () =>
				reader = loader.Load(long.MaxValue);

			It should_load_the_snapshot_with_the_highest_iteration = () =>
			{
				reader.Iteration.ShouldEqual(int.Parse(LaterIteration));
				reader.Read().First().ShouldBeLike(FirstRecord);
			};

			static readonly byte[] FirstRecord = BitConverter.GetBytes(42);
		}

		Establish context = () =>
		{
			file = Substitute.For<FileBase>();
			directory = Substitute.For<DirectoryBase>();
			loader = new SnapshotLoader(directory, file, Path);
		};

		const string Path = "./path/to/snapshots/";
		const string EarlierIteration = "1";
		const string LaterIteration = "2";
		const int MessageSequence = 42;
		static SnapshotLoader loader;
		static SnapshotStreamReader reader;
		static DirectoryBase directory;
		static FileBase file;
	}

	[Subject(typeof(SnapshotLoader))]
	public class when_loading_snapshots_using_parameterized_constraints
	{
		public class when_loading_a_snapshot_based_on_message_sequence
		{
			Establish context = () =>
			{
				var oneRecord = BitConverter.GetBytes(1);
				var firstRecordLength = BitConverter.GetBytes(4);
				var contents = oneRecord.Concat(firstRecordLength).Concat(FirstRecord).ToArray();
				var hash = new SoapHexBinary(new SHA1Managed().ComputeHash(contents));
				var earlierPath = Path + EarlierIteration + "-" + EarlySnapshotSequence + "-" + hash;
				var laterPath = Path + EarlierIteration + "-" + LaterSnapshotSequence + "-" + hash;

				directory
					.GetFiles(Path, "*", SearchOption.TopDirectoryOnly)
					.Returns(new[] { laterPath, earlierPath });

				file.OpenRead(earlierPath).Returns(new MemoryStream(contents));
				file.OpenRead(laterPath).Returns(new MemoryStream(contents));
			};

			Because of = () =>
				reader = loader.Load(StoredMessageSequence);

			It should_load_the_snapshot_whose_message_sequence_is_at_or_lower_the_provided_sequence = () =>
			{
				reader.MessageSequence.ShouldEqual(EarlySnapshotSequence);
				reader.Read().First().ShouldBeLike(FirstRecord);
			};

			const long EarlySnapshotSequence = StoredMessageSequence;
			const long LaterSnapshotSequence = StoredMessageSequence + 1;
		}

		public class when_loading_a_snapshot_based_on_snapshot_iteration
		{
			Establish context = () =>
			{
				var oneRecord = BitConverter.GetBytes(1);
				var firstRecordLength = BitConverter.GetBytes(4);
				var contents = oneRecord.Concat(firstRecordLength).Concat(FirstRecord).ToArray();
				var hash = new SoapHexBinary(new SHA1Managed().ComputeHash(contents));
				var earlierPath = Path + EarlierIteration + "-" + StoredMessageSequence + "-" + hash;
				var laterPath = Path + LaterIteration + "-" + StoredMessageSequence + "-" + hash;

				directory
					.GetFiles(Path, "*", SearchOption.TopDirectoryOnly)
					.Returns(new[] { earlierPath, laterPath });

				file.OpenRead(earlierPath).Returns(x => { throw new DivideByZeroException("This code should NOT be executed!!"); });
				file.OpenRead(laterPath).Returns(new MemoryStream(contents));
			};

			Because of = () =>
				reader = loader.Load(long.MaxValue);

			It should_ignore_snapshots_with_iterations_lower_than_the_provided_iteration = () =>
			{
				reader.Iteration.ShouldEqual(int.Parse(LaterIteration));
				reader.Read().First().ShouldBeLike(FirstRecord);
			};
		}

		Establish context = () =>
		{
			file = Substitute.For<FileBase>();
			directory = Substitute.For<DirectoryBase>();
			loader = new SnapshotLoader(directory, file, Path);
		};

		const string Path = "./path/to/snapshots/";
		const string EarlierIteration = "1";
		const string LaterIteration = "2";
		const int StoredMessageSequence = 42;
		static readonly byte[] FirstRecord = BitConverter.GetBytes(42);
		static SnapshotLoader loader;
		static SnapshotStreamReader reader;
		static DirectoryBase directory;
		static FileBase file;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
