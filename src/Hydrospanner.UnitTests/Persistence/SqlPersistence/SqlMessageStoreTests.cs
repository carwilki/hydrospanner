#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using Machine.Specifications;
	using NSubstitute;
	using NSubstitute.Experimental;
	using Phases.Journal;

	[Subject(typeof(SqlMessageStore))]
	public class when_initializing_the_store
	{
		It should_throw_if_the_factory_is_null = () =>
			Catch.Exception(() => new SqlMessageStore(null, connectionString, writer, types)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_connection_string_is_null_or_empty = () =>
		{
			Catch.Exception(() => new SqlMessageStore(factory, null, writer, types)).ShouldBeOfType<ArgumentNullException>();
			Catch.Exception(() => new SqlMessageStore(factory, string.Empty, writer, types)).ShouldBeOfType<ArgumentNullException>();
			Catch.Exception(() => new SqlMessageStore(factory, "  \t\n", writer, types)).ShouldBeOfType<ArgumentNullException>();
		};

		It should_throw_if_the_writer_factory_is_null = () =>
			Catch.Exception(() => new SqlMessageStore(factory, connectionString, null, types)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_type_registrar_is_null = () =>
			Catch.Exception(() => new SqlMessageStore(factory, connectionString, writer, null)).ShouldBeOfType<ArgumentNullException>();

		It should_NOT_throw_if_all_values_are_acceptable = () =>
			Catch.Exception(() => new SqlMessageStore(factory, connectionString, writer, types)).ShouldBeNull();

		Establish context = () =>
		{
			factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
			connectionString = "My Connection String";
			writer = Substitute.For<SqlMessageStoreWriter>();
			types = new JournalMessageTypeRegistrar(new string[0]);
		};

		static DbProviderFactory factory;
		static string connectionString;
		static SqlMessageStoreWriter writer;
		static JournalMessageTypeRegistrar types;
	}

	[Subject(typeof(SqlMessageStore))]
	public class when_persisting_messages
	{
		public class when_the_items_are_null
		{
			Because of = () =>
				store.Save(null);

			It should_do_nothing = () =>
				Received.InOrder(() => { /* These aren't the method calls you're looking for--move along... */ });
		}

		public class when_there_are_no_items
		{
			Because of = () =>
				store.Save(new List<JournalItem>());

			It should_do_nothing = () =>
				Received.InOrder(() => { /* These aren't the method calls you're looking for--move along... */ });
		}

		public class when_there_is_an_error_during_writing
		{
			Establish context = () =>
			{
				items = new List<JournalItem> { new JournalItem() };
				ThreadExtensions.Freeze(n => nap = n);
				writer.Write(Arg.Do<IList<JournalItem>>(t =>
				{
					if (thrown)
						return;

					thrown = true;
					throw new DivideByZeroException();
				}));
			};

			Because of = () =>
				store.Save(items);

			It should_take_a_nap_and_retry = () =>
				nap.ShouldEqual(TimeSpan.FromSeconds(3));

			It should_retry_until_successful = () =>
				writer.Received(2).Write(items);

			It should_cleanup_before_the_retry = () =>
				writer.Received(1).Cleanup();

			static bool thrown;
			static List<JournalItem> items;
			static TimeSpan nap;
		}

		public class when_writing_is_successful
		{
			Establish context = () =>
				items = new List<JournalItem> { new JournalItem() };

			Because of = () =>
				store.Save(items);

			It should_persist_the_items = () =>
				writer.Received(1).Write(items);

			static List<JournalItem> items;
		}

		Establish context = () =>
		{
			factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
			connectionString = "My Connection String";
			writer = Substitute.For<SqlMessageStoreWriter>();
			types = new JournalMessageTypeRegistrar(new string[0]);
			store = new SqlMessageStore(factory, connectionString, writer, types);
		};

		static SqlMessageStore store;
		static DbProviderFactory factory;
		static string connectionString;
		static SqlMessageStoreWriter writer;
		static JournalMessageTypeRegistrar types;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414