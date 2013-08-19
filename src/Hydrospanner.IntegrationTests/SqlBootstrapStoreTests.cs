#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Data.Common;
	using Machine.Specifications;
	using Persistence;
	using Persistence.SqlPersistence;

	[Subject(typeof(SqlBootstrapStore))]
	public class when_loading_bootstrap_information : TestDatabase
	{
		public class and_the_constructor_has_invalid_arguments
		{
			It should_throw_when_the_factory_is_null = () =>
				Catch.Exception(() => new SqlBootstrapStore(null, "connection", 1)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_when_the_connection_is_empty = () =>
				Catch.Exception(() => new SqlBootstrapStore(factory, string.Empty, 1)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_duplicate_window_is_too_small = () =>
				Catch.Exception(() => new SqlBootstrapStore(factory, "connection", 0)).ShouldBeOfType<ArgumentOutOfRangeException>();
		}

		public class and_errors_occur
		{
			Establish context = TearDownDatabase;

			It should_take_a_nap_and_retry = () =>
				nap.ShouldEqual(TimeSpan.FromSeconds(5));

			It should_return_the_info = () =>
				result.ShouldBeLike(new BootstrapInfo(0, 0, new string[0], new Guid[0]));
		}

		public class and_the_information_is_NOT_present
		{
			It should_return_default_and_empty_information = () =>
				result.ShouldBeLike(new BootstrapInfo(0, 0, new string[0], new Guid[0]));
		}

		public class and_the_information_is_present
		{
			Establish context = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = @"
						INSERT INTO checkpoints SELECT 5 ON DUPLICATE KEY UPDATE dispatch = 5;
						INSERT INTO metadata SELECT 0, 'asdf' ON DUPLICATE KEY UPDATE metadata_id = 10, type_name = 'asdf';
						INSERT INTO metadata SELECT 1, 'qwer' ON DUPLICATE KEY UPDATE metadata_id = 11, type_name = 'qwer';
						INSERT INTO messages SELECT 1, 1, tobin('11111111-1111-1111-1111-111111111111'), 'a', 'a';
						INSERT INTO messages SELECT 2, 2, tobin('22222222-2222-2222-2222-222222222222'), 'b', 'b';
						INSERT INTO messages SELECT 3, 1, tobin('33333333-3333-3333-3333-333333333333'), 'c', 'c';
						INSERT INTO messages SELECT 4, 1, tobin('44444444-4444-4444-4444-444444444444'), 'd', 'd';
						INSERT INTO messages SELECT 5, 1, tobin('55555555-5555-5555-5555-555555555555'), 'e', 'e';
						INSERT INTO messages SELECT 6, 1, tobin('66666666-6666-6666-6666-666666666666'), 'f', 'f';";
					command.ExecuteNonQuery();
				}
			};

			It should_load_all_the_information = () =>
			{
				var types = new[] { "asdf", "qwer" };
				var foreignIds = new[]
				{
					Guid.Parse("66666666-6666-6666-6666-666666666666"),
					Guid.Parse("55555555-5555-5555-5555-555555555555"),
					Guid.Parse("44444444-4444-4444-4444-444444444444"),
					Guid.Parse("33333333-3333-3333-3333-333333333333"),
					Guid.Parse("22222222-2222-2222-2222-222222222222")
				};
				result.ShouldBeLike(new BootstrapInfo(6, 5, types, foreignIds));
			};
		}

		Establish context = () =>
		{
			ThreadExtensions.Freeze(x =>
			{
				nap = x;
				InitializeDatabase();
			});
			factory = DbProviderFactories.GetFactory(settings.ProviderName);
			store = new SqlBootstrapStore(factory, connectionString, ForeignIdsToLoad);
		};

		Because of = () =>
			result = store.Load();

		static BootstrapInfo result;
		static SqlBootstrapStore store;
		static TimeSpan nap;
		const int ForeignIdsToLoad = 5;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
