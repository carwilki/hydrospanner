#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Linq;
	using System.Text;
	using Machine.Specifications;
	using Persistence.SqlPersistence;
	using Phases.Journal;
	using Serialization;
	using Wireup;

	[Subject(typeof(SqlMessageStore))]
	public class when_saving_messages : TestDatabase
	{
		public class when_there_are_no_messages_to_save
		{
			It should_not_save_anything_to_the_database = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select count(*) from messages; select count(*) from metadata;";
					using (var reader = command.ExecuteReader())
					{
						reader.Read().ShouldBeTrue();
						reader.GetInt64(0).ShouldEqual(0L);
						reader.NextResult().ShouldBeTrue();
						reader.Read().ShouldBeTrue();
						reader.GetInt64(0).ShouldEqual(0L);
					}
				}
			};
		}

		public class when_an_error_occurs_while_trying_to_save
		{
			Establish context = () =>
			{
				TearDownDatabase();
				var item = new JournalItem();
				item.AsForeignMessage(0, new byte[] { 1 }, "hi", new Dictionary<string, string>(), Guid.NewGuid(), null);
				item.Serialize(new JsonSerializer());
				items.Add(item);
			};

			It should_take_a_nap_and_retry = () =>
				napTime.ShouldEqual(TimeSpan.FromSeconds(3));
		}

		public class when_all_goes_well
		{
			Establish context = () =>
			{
				var serializer = new JsonSerializer();

				var first = new JournalItem();
				first.AsForeignMessage(0, new byte[] { 1 }, "hi", new Dictionary<string, string> { { "key", "value" } }, ForeignId, x => { });
				first.Serialize(serializer);
				items.Add(first);
	
				var second = new JournalItem();
				second.AsTransformationResultMessage(1, "hi", new Dictionary<string, string> { { "key", "value" } });
				second.Serialize(serializer);
				items.Add(second);
			};

			It should_save_the_messages = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select * from messages;";
					using (var reader = command.ExecuteReader())
					{
						reader.Read().ShouldBeTrue();
						reader.GetInt64(0).ShouldEqual(0L);
						reader.GetInt16(1).ShouldEqual((short)1);
						var id = reader.GetValue(2);
						id.ShouldBeLike(ForeignId.ToByteArray());
						reader.GetValue(3).ShouldBeLike(new byte[] { 1 });
						reader.GetValue(4).ShouldBeLike(Encoding.UTF8.GetBytes("{\"key\":\"value\"}"));
						reader.Read().ShouldBeTrue();
						reader.GetInt64(0).ShouldEqual(1L);
						reader.GetInt16(1).ShouldEqual((short)1);
						reader.GetValue(2).ShouldEqual(DBNull.Value);
						reader.GetValue(3).ShouldBeLike(Encoding.UTF8.GetBytes("\"hi\""));
						reader.GetValue(4).ShouldBeLike(Encoding.UTF8.GetBytes("{\"key\":\"value\"}"));
						reader.Read().ShouldBeFalse();
					}
				}
			};

			It should_save_the_metadata = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select * from metadata;";
					using (var reader = command.ExecuteReader())
					{
						reader.Read().ShouldBeTrue();
						reader.GetInt16(0).ShouldEqual((short)1);
						reader.GetString(1).ShouldEqual(string.Empty.ResolvableTypeName());
						reader.Read().ShouldBeFalse();
					}
				}
			};

			static readonly Guid ForeignId = Guid.NewGuid();
		}

		public class when_saving_a_duplicate_message
		{
			Establish context = () =>
			{
				item = new JournalItem();
				item.AsForeignMessage(42, new byte[] { 1, 2, 3, 4, 5, 6 }, null, new Dictionary<string, string>(), Guid.NewGuid(), null);
				item.SerializedType = "some-type";
				items.Add(item);

				Persist();
			};

			It should_only_record_the_message_once = () =>
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select count(*) from messages;";
					((long)command.ExecuteScalar()).ShouldEqual(1);
				}
			};

			static JournalItem item;
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
			Persist();

		static void Persist()
		{
			var factory = DbProviderFactories.GetFactory(settings.ProviderName);
			var types = new JournalMessageTypeRegistrar(new string[0]);
			var session = new SqlBulkInsertSession(factory, settings.ConnectionString);
			var builder = new SqlBulkInsertCommandBuilder(types, session);
			var writer = new SqlMessageStoreWriter(session, builder, types, 100);
			store = new SqlMessageStore(factory, settings.ConnectionString, writer, types);
			store.Save(items);
		}

		static List<JournalItem> items;
		static SqlMessageStore store;
	}

	[Subject(typeof(SqlMessageStore))]
	public class when_loading_journaled_messages : TestDatabase
	{
		public class when_an_error_occurs_when_loading
		{
			Establish context = () =>
				results = new List<JournaledMessage>();

			Because of = () =>
			{
				messages = store.Load(1);
				enumerator = messages.GetEnumerator();

				var index = 0;
				foreach (var message in messages)
				{
					results.Add(message);
					if (index++ == 1)
						((SqlMessageStoreReader)enumerator).Dispose();
				}
			};

			It should_attempt_the_load_until_successful = () =>
				((SqlMessageStoreReader)enumerator).ConnectionAttempts.ShouldEqual(2);

			It should_have_loaded_the_journal_messages = () =>
				results.ShouldBeLike(new[]
				{
					new JournaledMessage
					{
						Sequence = 1,
						SerializedBody = serializer.Serialize(42),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName()
					},
					new JournaledMessage
					{
						Sequence = 2,
						SerializedBody = serializer.Serialize(43),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName(),
						ForeignId = foreignId
					},
					new JournaledMessage
					{
						Sequence = 3,
						SerializedBody = serializer.Serialize(44),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName()
					},
					new JournaledMessage
					{
						Sequence = 4,
						SerializedBody = serializer.Serialize(45),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName()
					}
				});

			static IEnumerator<JournaledMessage> enumerator; 
			static IEnumerable<JournaledMessage> messages;
			static List<JournaledMessage> results; 
			static JournaledMessage loadedFirst;
			static JournaledMessage loadedSecond;
			static JournaledMessage loadedLast;
		}

		public class when_loading_goes_well
		{
			Because of = () =>
				results = store.Load(2).ToList();

			It should_load_all_messages_at_or_above_the_given_sequence = () =>
				results.ShouldBeLike(new[]
				{
					new JournaledMessage
					{
						Sequence = 2,
						SerializedBody = serializer.Serialize(43),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName(),
						ForeignId = foreignId
					},
					new JournaledMessage
					{
						Sequence = 3,
						SerializedBody = serializer.Serialize(44),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName()
					},
					new JournaledMessage
					{
						Sequence = 4,
						SerializedBody = serializer.Serialize(45),
						SerializedHeaders = null,
						SerializedType = 0.ResolvableTypeName()
					}
				});
		
			static List<JournaledMessage> results;
		}

		Establish context = () =>
		{
			var factory = DbProviderFactories.GetFactory(settings.ProviderName);
			var types = new JournalMessageTypeRegistrar(new string[0]);
			var session = new SqlBulkInsertSession(factory, settings.ConnectionString);
			var builder = new SqlBulkInsertCommandBuilder(types, session);
			var writer = new SqlMessageStoreWriter(session, builder, types, 100);
			store = new SqlMessageStore(factory, settings.ConnectionString, writer, types);
			serializer = new JsonSerializer();

			first = new JournalItem();
			first.AsTransformationResultMessage(1, 42, null);
			first.Serialize(serializer);

			second = new JournalItem();
			second.AsForeignMessage(2, serializer.Serialize(43), 43, null, foreignId, x => { });
			second.Serialize(serializer);

			third = new JournalItem();
			third.AsTransformationResultMessage(3, 44, null);
			third.Serialize(serializer);

			fourth = new JournalItem();
			fourth.AsTransformationResultMessage(4, 45, null);
			fourth.Serialize(serializer);

			store.Save(new List<JournalItem> { first, second, third, fourth });
		};

		static JournalItem first;
		static JournalItem third;
		static JournalItem second;
		static JournalItem fourth;
		static JsonSerializer serializer;
		static SqlMessageStore store;
		static readonly Guid foreignId = Guid.NewGuid();
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
