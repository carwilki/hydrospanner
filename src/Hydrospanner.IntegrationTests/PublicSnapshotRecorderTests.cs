#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Globalization;
	using Machine.Specifications;
	using Phases.Snapshot;
	using Serialization;

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_a_snapshot_is_recorded : TestDatabase
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

	[Ignore("Long running...")]
	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_there_are_many_snapshots : TestDatabase
	{
		Establish context = () =>
		{
			serializer = new JsonSerializer();
			recorder = new PublicSnapshotRecorder(settings);
		};

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
				int.Parse(command.ExecuteScalar().ToString()).ShouldEqual(Snapshots);
			}
		};

		static SnapshotItem Generate(string key, string value, long sequence)
		{
			var item = new SnapshotItem();
			item.AsPublicSnapshot(key, value, sequence);
			item.Serialize(serializer);
			return item;
		}

		static JsonSerializer serializer;
		static PublicSnapshotRecorder recorder;
		const int Snapshots = 20000;
	}

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_there_are_no_snapshots_to_record : TestDatabase
	{
		Establish context = () =>
		{
			recorder = new PublicSnapshotRecorder(null);

			ThreadExtensions.Freeze(x =>
			{
				napTime = x;
				InitializeDatabase();
			});
		};

		Because of = () =>
		{
			recorder.StartRecording(1);
			Catch.Exception(() => recorder.FinishRecording()).ShouldBeNull();
		};

		It should_not_perform_any_database_actions = () =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select count(*) from `hydrospanner-test`.`documents`;";
				int.Parse(command.ExecuteScalar().ToString()).ShouldEqual(0);
			}
		};
	
		static PublicSnapshotRecorder recorder;
	}

	[Subject(typeof(PublicSnapshotRecorder))]
	public class when_an_error_happen : TestDatabase
	{
		Establish context = () =>
		{
			TearDownDatabase();
			recorder = new PublicSnapshotRecorder(settings);

			snapshotItem = new SnapshotItem();
			snapshotItem.AsPublicSnapshot("key", "memento", 42);
			snapshotItem.Serialize(new JsonSerializer());

			ThreadExtensions.Freeze(x =>
			{
				napTime = x;
				InitializeDatabase();
			});
		};

		Because of = () =>
		{
			recorder.StartRecording(1);
			recorder.Record(snapshotItem);
			recorder.FinishRecording();
		};

		It should_take_a_nap_and_retry = () =>
			napTime.ShouldEqual(TimeSpan.FromSeconds(5));

		It should_succeed_upon_retry = () =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "select count(*) from `hydrospanner-test`.`documents`;";
				int.Parse(command.ExecuteScalar().ToString()).ShouldEqual(1);
			}
		};
	
		static PublicSnapshotRecorder recorder;
		static SnapshotItem snapshotItem;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
