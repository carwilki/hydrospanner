#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using Machine.Specifications;
	using Phases.Snapshot;
	using Serialization;

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_a_record_is_recorded : TestDatabase
	{
		Establish context = () =>
		{
			ThreadExtensions.Freeze(x => { });
			item = new SnapshotItem();
			item.AsPublicSnapshot("key", "value", 1);
			item.Serialize(new JsonSerializer());
			recorder = new PublicSnapshotRecorder(settings);
		};

		Because of = () =>
		{
			recorder.StartRecording(1);
			recorder.Record(item);
			recorder.FinishRecording();
		};

		It should_be_added_to_storage = () =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select * from `hydrospanner-test`.`documents`;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().ShouldBeTrue();
					reader.GetString(0).ShouldEqual("key");
					reader.GetInt64(1).ShouldEqual(1);
					reader.GetInt32(2).ShouldBeGreaterThan(0);
					(reader.GetValue(3) as byte[]).ShouldBeLike(item.Serialized);
				}
			}
		};

		static PublicSnapshotRecorder recorder;
		static SnapshotItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
