#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using Machine.Specifications;
	using NSubstitute;
	using Phases.Journal;
	using Serialization;

	[Subject(typeof(SqlMessageStoreWriter))]
	public class when_initializing_the_writer
	{
		It should_throw_if_the_session_factory_is_null = () =>
			Catch.Exception(() => new SqlMessageStoreWriter(null, builder, types, MaxSliceSize)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_builder_is_null = () =>
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, null, types, MaxSliceSize)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_type_registrar_is_null = () =>
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, null, MaxSliceSize)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_slice_size_is_out_of_range = () =>
		{
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, types, 9)).ShouldBeOfType<ArgumentOutOfRangeException>();
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, types, 0)).ShouldBeOfType<ArgumentOutOfRangeException>();
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, types, -1)).ShouldBeOfType<ArgumentOutOfRangeException>();
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, types, int.MinValue)).ShouldBeOfType<ArgumentOutOfRangeException>();
		};

		It should_NOT_throw_if_all_parameters_are_valid = () =>
			Catch.Exception(() => new SqlMessageStoreWriter(sessionFactory, builder, types, MaxSliceSize)).ShouldBeNull();

		Establish context = () =>
		{
			sessionFactory = () => null;
			builder = Substitute.For<SqlBulkInsertCommandBuilder>();
			types = Substitute.For<JournalMessageTypeRegistrar>();
		};

		static Func<SqlBulkInsertSession> sessionFactory;
		static SqlBulkInsertCommandBuilder builder;
		static JournalMessageTypeRegistrar types;
		const int MaxSliceSize = 42;
	}

	[Subject(typeof(SqlMessageStoreWriter))]
	public class when_writing_messages
	{
		const int MaxSliceSize = 10;

		public class when_there_is_a_single_batch_of_items
		{
			Establish context = () =>
			{
				parameterNames = new List<string>();
				parameterValues = new List<object>();
				session.IncludeParameter(Arg.Do<string>(parameterNames.Add), Arg.Do<object>(parameterValues.Add));
				session.ExecuteCurrentCommand(Arg.Do<string>(x => commandText = x));

				local = new JournalItem();
				local.AsTransformationResultMessage(41, "asdf", new Dictionary<string, string>());
				local.Serialize(new JsonSerializer());
				
				foreign = new JournalItem();
				foreign.AsForeignMessage(
					42, Encoding.UTF8.GetBytes("42"), 42, new Dictionary<string, string>(), Guid.NewGuid(), () => { });
				
				items.Add(local);
				items.Add(foreign);
			};

			Because of = () =>
				writer.Write(items);

			It should_write_all_items_in_a_transaction = () =>
			{
				session.Received(1).BeginTransaction();
				session.Received(1).PrepareNewCommand();
				session.Received(1).ExecuteCurrentCommand(Arg.Any<string>());
				session.Received(1).CommitTransaction();
			};

			It should_save_them_all_in_the_same_batch = () =>
			{
				commandText.Trim().Replace("\n", string.Empty).ShouldEqual(
					"INSERT INTO metadata SELECT 1, @t1;" +
					"INSERT INTO messages SELECT 41, 1, NULL, @p0, @h0;" +
					"INSERT INTO metadata SELECT 2, @t2;" +
					"INSERT INTO messages SELECT 42, 2, @f1, @p1, @h1;");
				
				parameterNames.ShouldBeLike(new[] { "@p0", "@h0", "@t1", "@p1", "@h1", "@t2", "@f1" });
				
				parameterValues.ShouldBeLike(new object[]
				{
					local.SerializedBody, 
					local.SerializedHeaders, 
					local.SerializedType, 
					
					foreign.SerializedBody, 
					null, // we don't save empty headers on foreign messages
					foreign.SerializedType, 
					foreign.ForeignId.ToByteArray()
				});
			};

			static string commandText;
			static List<string> parameterNames;
			static List<object> parameterValues;
			static JournalItem local;
			static JournalItem foreign;
		}

		public class when_there_are_enough_items_to_constitute_multiple_batches
		{
			Establish context = () =>
			{
				for (var i = 0; i < MaxSliceSize * 3; i++)
					items.Add(Next());
			};

			Because of = () =>
				writer.Write(items);

			It should_write_all_items_in_a_transaction_using_multiple_batches = () =>
			{
				session.Received(1).BeginTransaction();
				session.Received(3).PrepareNewCommand();
				session.Received(3).ExecuteCurrentCommand(Arg.Any<string>());
				session.Received(1).CommitTransaction();
			};

			static JournalItem Next()
			{
				var item = new JournalItem();

				if (sequence++ % 2 == 0)
					item.AsForeignMessage(sequence, Body(), sequence, Headers, Guid.NewGuid(), () => { });
				else
					item.AsTransformationResultMessage(sequence, sequence, Headers);

				item.Serialize(Serializer);

				return item;
			}

			static readonly JsonSerializer Serializer = new JsonSerializer();
			static readonly Func<byte[]> Body = () => Encoding.UTF8.GetBytes(sequence.ToString(CultureInfo.InvariantCulture));
			static readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
			static long sequence;
		}

		Establish context = () =>
		{
			session = Substitute.For<SqlBulkInsertSession>();
			types = new JournalMessageTypeRegistrar(new string[0]);
			builder = new SqlBulkInsertCommandBuilder(types, session);
			writer = new SqlMessageStoreWriter(SessionFactory, builder, types, MaxSliceSize);
			items = new List<JournalItem>();
		};

		static SqlBulkInsertSession SessionFactory()
		{
			return session;
		}

		static SqlBulkInsertSession session;
		static SqlBulkInsertCommandBuilder builder;
		static JournalMessageTypeRegistrar types;
		static SqlMessageStoreWriter writer;
		static List<JournalItem> items;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
