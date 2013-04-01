#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Abstractions;
	using System.Linq;
	using Machine.Specifications;
	using Phases.Snapshot;
	using Serialization;

	[Subject(typeof(SystemSnapshotRecorder))]
	public class when_writing_a_system_snapshot
	{
		Establish context = () =>
		{
			serializer = new JsonSerializer();
			recorder = new SystemSnapshotRecorder(new FileWrapper(), workingDirectory);
			expectedRecords = Enumerable.Range(1, 10)
				.Select(x => new KeyValuePair<string, byte[]>(typeof(int).AssemblyQualifiedName, serializer.Serialize(x)))
				.ToList();
		};

		Because of = () =>
		{
			recorder.StartRecording(10);
			for (var i = 1; i <= 10; i++)
				recorder.Record(Generate(i, i, 10 - i));

			recorder.FinishRecording(42, 10);
		};

		It should_create_a_snapshot_file_with_the_appropriate_values = () =>
		{
			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), workingDirectory);
			using (var reader = loader.Load(10, 42))
			{
				reader.Count.ShouldEqual(10);
				reader.Generation.ShouldEqual(42);
				reader.MessageSequence.ShouldEqual(10);
				var records = reader.Read().ToList();
				for (var i = 0; i < records.Count; i++)
				{
					var expected = expectedRecords[i];
					var actual = records[i];
					actual.ShouldBeEqual(expected);
				}
			}
		};

		static SnapshotItem Generate(int value, long sequence, int remaining)
		{
			var item = new SnapshotItem();
			item.AsPartOfSystemSnapshot(sequence, remaining, value);
			item.Serialize(serializer);
			return item;
		}

		static SystemSnapshotRecorder recorder;
		static JsonSerializer serializer;
		static readonly string workingDirectory = Directory.GetCurrentDirectory();
		static List<KeyValuePair<string, byte[]>> expectedRecords;
	}

	public static class ShouldExtensions
	{
		public static void ShouldBeEqual(this KeyValuePair<string, byte[]> pair, KeyValuePair<string, byte[]> other)
		{
			pair.Key.ShouldEqual(other.Key);
			pair.Value.ShouldBeLike(other.Value);
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
