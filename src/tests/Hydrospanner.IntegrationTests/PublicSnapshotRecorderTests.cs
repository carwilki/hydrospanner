#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System.Globalization;
	using Machine.Specifications;
	using Phases.Snapshot;
	using Serialization;

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_a_record_is_recorded : TestDatabase
	{
		Establish context = () =>
		{
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

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_there_are_enough_snapshots_to_necessitate_multiple_batches : TestDatabase
	{
		Establish context = () =>
			recorder = new PublicSnapshotRecorder(settings);

		Because of = () =>
		{
			recorder.StartRecording(Snapshots);

			for (var x = 0; x < Snapshots; x++)
				recorder.Record(Generate(x.ToString(CultureInfo.InvariantCulture), x.ToString(CultureInfo.InvariantCulture), x));

			recorder.FinishRecording();
		};

		It should_write_multiple_batches_to_insert_all_records = () =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select count(*) from `hydrospanner-test`.`documents`;";
				using (var reader = command.ExecuteReader())
				{
					reader.Read().ShouldBeTrue();
					reader.GetInt32(0).ShouldEqual(Snapshots);
				}
			}
		};

		static SnapshotItem Generate(string key, string value, long sequence)
		{
			var item = new SnapshotItem();
			item.AsPublicSnapshot(key, value, sequence);
			item.Serialize(new JsonSerializer());
			return item;
		}
		
		static PublicSnapshotRecorder recorder;
		const int Snapshots = 20000;
	}

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_database_errors_happen
	{
		Establish context = () =>
		{

		};

		Because of = () =>
		{

		};

		It should_take_a_nap_and_retry;
	
		static PublicSnapshotRecorder recorder;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
