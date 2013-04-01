#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Data.Common;
	using Machine.Specifications;
	using Persistence.SqlPersistence;

	[Subject(typeof(SqlCheckpointStore))]
	public class when_saving_the_current_checkpoint : TestDatabase
	{
		public class when_the_sequence_is_NOT_positive
		{
			Because of = () =>
				store.Save(0);

			It should_not_persist_the_sequence = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select dispatch from checkpoints;";
					command.ExecuteScalar().ShouldEqual(0L);
				}
			};
		}

		public class when_an_error_occurs_during_persistence
		{
			Establish context = () =>
			{
				TearDownDatabase();
				ThreadExtensions.Freeze(x =>
				{
					nap = x;
					InitializeDatabase();
				});
			};

			Because of = () =>
				store.Save(42);

			It should_take_a_nap_and_retry = () =>
				nap.ShouldEqual(TimeSpan.FromSeconds(5));

			It should_persist_the_sequence = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select dispatch from checkpoints;";
					command.ExecuteScalar().ShouldEqual(42L);
				}
			};
		}

		public class when_saving_is_completed
		{
			Because of = () =>
				store.Save(19);

			It should_persist_the_checkpoint_in_the_database = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select dispatch from checkpoints;";
					command.ExecuteScalar().ShouldEqual(19L);
				}
			};
		}

		public class when_saving_a_lower_checkpoint_than_is_currently_saved
		{
			Establish context = () => 
				store.Save(42);

			Because of = () => 
				store.Save(41);

			It should_NOT_update_the_checkpoint = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select dispatch from checkpoints;";
					command.ExecuteScalar().ShouldEqual(42L);
				}
			};
		}

		Establish context = () =>
		{
			InitializeDatabase();
			ThreadExtensions.Freeze(x => nap = x);
			var factory = DbProviderFactories.GetFactory(settings.ProviderName);
			store = new SqlCheckpointStore(factory, settings.ConnectionString);
		};

		static TimeSpan nap;
		static SqlCheckpointStore store;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
