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
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(Prefix)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { "hi", "not-a-snapshot", "blah-blah-blah" });

			It should_return_a_blank_snapshot = () =>
				reader.Count.ShouldEqual(0);
		}

		public class and_there_are_no_viable_snapshots
		{
			Establish context = () =>
			{
				file.OpenRead(Arg.Any<string>()).Returns(new MemoryStream(new byte[] { 0, 0, 0, 0 }));
				directory
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(Prefix)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { Path + Prefix + "-" + MessageSequence + "-bad_hash" });
			};

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
				var path = Path + Prefix + "-" + MessageSequence + "-" + hash;

				directory
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(Prefix)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { path });
				
				file.OpenRead(path)
					.Returns(new MemoryStream(contents));
			};

			It should_load_the_most_recent_viable_snapshot = () =>
				reader.Read().First().ShouldBeLike(FirstRecord);

			static readonly byte[] FirstRecord = BitConverter.GetBytes(42);
		}

		Establish context = () =>
		{
			file = Substitute.For<FileBase>();
			directory = Substitute.For<DirectoryBase>();
			loader = new SnapshotLoader(directory, file, Path, Prefix);
		};

		Because of = () =>
			reader = loader.LoadMostRecent();

		const string Path = "./path/to/snapshots/";
		const string Prefix = "1";
		const int MessageSequence = 42;
		static SnapshotLoader loader;
		static SnapshotStreamReader reader;
		static DirectoryBase directory;
		static FileBase file;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
