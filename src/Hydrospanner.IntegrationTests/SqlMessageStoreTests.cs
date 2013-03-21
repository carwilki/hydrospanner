#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System.Collections.Generic;
	using System.Data.Common;
	using Machine.Specifications;
	using Persistence.SqlPersistence;
	using Phases.Journal;

	[Subject(typeof(SqlMessageStore))]
	public class when_saving_messages : TestDatabase
	{
		public class when_there_are_no_messages_to_save
		{
			It should_not_save_anything_to_the_database;
		}

		public class when_an_error_occurs_while_trying_to_save
		{
			It should_take_a_nap_and_retry;

			It should_save_the_messages;

			It should_save_the_metadata;
		}

		public class when_all_goes_well
		{
			It should_save_the_messages;

			It should_save_the_metadata;
		}

		Establish context = () =>
		{
			ThreadExtensions.Freeze(x =>
			{
				napTime = x;
				InitializeDatabase();
			});
			items = new List<JournalItem>();
		};

		Because of = () =>
		{
			var factory = DbProviderFactories.GetFactory(settings.ProviderName);
			store = new SqlMessageStore(factory, settings.ConnectionString, null);
			store.Save(items);
		};

		static List<JournalItem> items;
		static SqlMessageStore store;
	}

	[Subject(typeof(SqlMessageStore))]
	public class when_loading_journaled_messages : TestDatabase
	{
		public class when_an_error_occurs_when_loading
		{
			It should_take_a_nap_and_retry;

			It should_load_all_local_messages_below_the_given_sequence;
		}

		public class when_loading_goes_well
		{
			It should_load_all_local_messages_below_the_given_sequence;
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
