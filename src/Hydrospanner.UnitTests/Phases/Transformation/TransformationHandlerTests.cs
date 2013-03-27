#pragma warning disable 169 
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Journal;
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(TransformationHandler))]
	public class when_transforming_hydratables_based_on_incoming_messgaes
	{
		public class when_initialization_parameters_are_null
		{
			It should_throw_if_the_sequence_is_out_of_range = () =>
			{
				Try(() => new TransformationHandler(0, journal, duplicates, transformer, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new TransformationHandler(-1, journal, duplicates, transformer, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new TransformationHandler(long.MinValue, journal, duplicates, transformer, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_journal_is_null = () =>
				Try(() => new TransformationHandler(1, null, duplicates, transformer, snapshot)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_duplicates_is_null = () =>
				Try(() => new TransformationHandler(1, journal, null, transformer, snapshot)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_transformer_is_null = () =>
				Try(() => new TransformationHandler(1, journal, duplicates, null, snapshot)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_snapshot_is_null = () =>
				Try(() => new TransformationHandler(1, journal, duplicates, transformer, null)).ShouldBeOfType<ArgumentNullException>();

			static Exception Try(Action action)
			{
				return Catch.Exception(action);
			}
		}

		public class when_the_message_is_a_duplicate
		{
			Establish context = () =>
			{
				duplicates.Forward(item).Returns(true);
				handler = new TransformationHandler(2, journal, duplicates, transformer, snapshot);
			};

			Because of = () =>
				handler.OnNext(item, 0, false);

			It should_NOT_use_the_message_for_transformation = () =>
				transformer.DidNotReceive().Handle(item, Arg.Any<Dictionary<string, string>>(), Arg.Any<bool>());

			It should_NOT_increment_the_snapshot = () =>
				snapshot.DidNotReceive().Increment(Arg.Any<int>());

			It should_NOT_publish_the_message_to_the_journal_ring = () =>
				journal.AllItems.ShouldBeEmpty();
		}

		public class when_the_message_is_handled_during_replay
		{
			const bool ReplayValue = false;

			public class when_the_message_yields_NO_resulting_messages
			{
				Establish context = () =>
				{
					transformer.Handle(item.Body, item.Headers, ReplayValue).Returns(new object[0]);
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 1;
					item.Deserialize(new JsonSerializer());
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_anything_else = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_one = () =>
					snapshot.Received().Increment(1);

				It should_NOT_assign_the_message_sequence_on_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);
			}
			
			public class when_the_message_yeilds_a_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 1;
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, ReplayValue).Returns(new object[] { "hello", "world" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[0]);
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Increment(3);

				It should_NOT_reassign_the_sequence_number_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);
			}

			public class when_the_yielded_messages_yield_more_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 1;
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, ReplayValue).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_publish_the_incoming_message_and_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Increment(3);

				It should_NOT_reassign_the_message_sequence_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);
			}

			public class when_a_subsequent_message_is_received
			{
				Establish context = () =>
				{
					var serializer = new JsonSerializer();
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 2;
					item.Deserialize(serializer);
					item2 = new TransformationItem();
					item2.AsForeignMessage(Encoding.UTF8.GetBytes("2"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item2.MessageSequence = JournaledSequence - 1;
					item2.Deserialize(serializer);

					transformer.Handle(item.Body, item.Headers, ReplayValue).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayValue).Returns(new object[0]);
					transformer.Handle(item2.Body, item2.Headers, ReplayValue).Returns(new object[0]);
					
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
					handler.OnNext(item, 1, false);
				};

				Because of = () =>
					handler.OnNext(item2, 2, false);

				It should_track_the_sequence_number_correctly = () => 
					journal.AllItems.ShouldBeEmpty();

				It should_NOT_reassign_the_message_sequences_for_the_incoming_messages = () =>
				{
					item.MessageSequence.ShouldEqual(JournaledSequence - 2);
					item2.MessageSequence.ShouldEqual(JournaledSequence - 1);
				};

				static TransformationItem item2;
			}
		}
		
		public class when_the_message_is_from_the_live_stream
		{
			const bool LiveValue = true;

			public class when_the_message_yields_NO_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_publish_the_incoming_message_and_nothing_else = () =>
					journal.AllItems.Single().ShouldBeLike(new JournalItem
					{
						Body = item.Body,
						ForeignId = item.ForeignId,
						SerializedBody = item.SerializedBody,
						ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal,
						MessageSequence = JournaledSequence + 1,
						SerializedType = item.SerializedType,
					});

				It should_increment_the_snapshot_by_one = () =>
					snapshot.Received().Increment(1);

				It should_assign_the_message_sequence_on_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_the_message_yeilds_a_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveValue).Returns(new object[] { "hello", "world" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[0]);
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_publish_the_incoming_message_and_the_resulting_messages = () =>
					journal.AllItems.ShouldBeLike(new[]
					{
						new JournalItem
						{
							Body = item.Body,
							ForeignId = item.ForeignId,
							SerializedBody = item.SerializedBody,
							ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal,
							MessageSequence = JournaledSequence + 1,
							SerializedType = item.SerializedType
						},
						new JournalItem
						{
							Body = "hello",
							MessageSequence = JournaledSequence + 2, 
							ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
							SerializedType = typeof(string).AssemblyQualifiedName
						},
						new JournalItem
						{
							Body = "world",
							MessageSequence = JournaledSequence + 3, 
							ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
							SerializedType = typeof(string).AssemblyQualifiedName
						}
					});

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Increment(3);

				It should_assign_the_message_sequence_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_the_yielded_messages_yield_more_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveValue).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_publish_the_incoming_message_and_the_resulting_messages = () =>
					journal.AllItems.ShouldBeLike(new[]
				{
					new JournalItem
					{
						Body = item.Body,
						ForeignId = item.ForeignId,
						SerializedBody = item.SerializedBody,
						ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal,
						MessageSequence = JournaledSequence + 1,
						SerializedType = item.SerializedType
					},
					new JournalItem
					{
						Body = "hello",
						MessageSequence = JournaledSequence + 2, 
						ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
						SerializedType = typeof(string).AssemblyQualifiedName
					},
					new JournalItem
					{
						Body = "world",
						MessageSequence = JournaledSequence + 3, 
						ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
						SerializedType = typeof(string).AssemblyQualifiedName
					}
				});

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Increment(3);

				It should_assign_the_message_sequence = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_a_subsequent_message_is_received
			{
				Establish context = () =>
				{
					var serializer = new JsonSerializer();
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item.Deserialize(serializer);
					item2 = new TransformationItem();
					item2.AsForeignMessage(Encoding.UTF8.GetBytes("2"), typeof(int).AssemblyQualifiedName, null, Guid.NewGuid(), null);
					item2.Deserialize(serializer);

					transformer.Handle(item.Body, item.Headers, LiveValue).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveValue).Returns(new object[0]);
					transformer.Handle(item2.Body, item.Headers, LiveValue).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, duplicates, transformer, snapshot);
					handler.OnNext(item, 1, false);
				};

				Because of = () =>
					handler.OnNext(item2, 2, false);

				It should_track_the_sequence_number_correctly = () => journal.AllItems.ShouldBeLike(new[]
				{
					new JournalItem
					{
						Body = item.Body,
						ForeignId = item.ForeignId,
						SerializedBody = item.SerializedBody,
						ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal,
						MessageSequence = JournaledSequence + 1,
						SerializedType = item.SerializedType
					},
					new JournalItem
					{
						Body = "hello",
						MessageSequence = JournaledSequence + 2,
						ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
						SerializedType = typeof(string).AssemblyQualifiedName
					},
					new JournalItem
					{
						Body = "world",
						MessageSequence = JournaledSequence + 3,
						ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
						SerializedType = typeof(string).AssemblyQualifiedName
					},
					new JournalItem
					{
						Body = item2.Body,
						ForeignId = item2.ForeignId,
						SerializedBody = item2.SerializedBody,
						ItemActions = JournalItemAction.Acknowledge | JournalItemAction.Journal,
						MessageSequence = JournaledSequence + 4,
						SerializedType = item2.SerializedType
					}
				});

				It should_assign_the_sequence_numbers_of_the_incoming_messages = () =>
				{
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
					item2.MessageSequence.ShouldEqual(JournaledSequence + 4);
				};

				static TransformationItem item2;
			}
		}
		
		Establish context = () =>
		{
			item = new TransformationItem();
			journal = new RingBufferHarness<JournalItem>();
			duplicates = Substitute.For<IDuplicateHandler>();
			transformer = Substitute.For<ITransformer>();
			snapshot = Substitute.For<ISnapshotTracker>();
		};

		const long JournaledSequence = 42;
		static TransformationItem item;
		static TransformationHandler handler;
		static RingBufferHarness<JournalItem> journal;
		static IDuplicateHandler duplicates;
		static ITransformer transformer;
		static ISnapshotTracker snapshot;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169