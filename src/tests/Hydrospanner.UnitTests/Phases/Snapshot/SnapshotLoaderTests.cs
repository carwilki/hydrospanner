#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Abstractions;
	using System.IO.Abstractions.TestingHelpers;
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
				reader.ShouldNotBeNull(); // TODO: more verification here
		}

		public class and_there_are_no_viable_snapshots
		{
			Establish context = () =>
				directory
					.GetFiles(Path, Arg.Is<string>(x => x.StartsWith(Prefix)), SearchOption.TopDirectoryOnly)
					.Returns(new[] { Path + Prefix + "-bad_hash" });

			It should_return_a_blank_snapshot;
		}

		public class and_there_are_viable_snapshots
		{
			Establish context = () =>
			{

			};

			It should_load_the_most_recent_snapshot;
		}

		Establish context = () =>
		{
			directory = Substitute.For<DirectoryBase>();
			loader = new SnapshotLoader(directory, Path, Prefix);
		};

		Because of = () =>
			reader = loader.LoadMostRecent();

		const string Path = "./path/to/snapshots";
		const string Prefix = "prefix";
		static SnapshotLoader loader;
		static SnapshotStreamReader reader;
		static DirectoryBase directory;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
