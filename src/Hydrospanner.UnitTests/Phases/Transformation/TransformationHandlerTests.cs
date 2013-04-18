#pragma warning disable 169, 414 
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
				Try(() => new TransformationHandler(-1, journal, transformer, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new TransformationHandler(long.MinValue, journal, transformer, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_journal_is_null = () =>
			{
				Try(() => new TransformationHandler(0, null, transformer, snapshot)).ShouldBeOfType<ArgumentNullException>();
				Try(() => new TransformationHandler(1, null, transformer, snapshot)).ShouldBeOfType<ArgumentNullException>();
				Try(() => new TransformationHandler(int.MaxValue, null, transformer, snapshot)).ShouldBeOfType<ArgumentNullException>();
			};

			It should_throw_if_the_transformer_is_null = () =>
				Try(() => new TransformationHandler(1, journal, null, snapshot)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_snapshot_is_null = () =>
				Try(() => new TransformationHandler(1, journal, transformer, null)).ShouldBeOfType<ArgumentNullException>();

			static Exception Try(Action action)
			{
				return Catch.Exception(action);
			}
		}

		public class when_a_message_with_no_body_arrives
		{
			Establish context = () =>
				handler = new TransformationHandler(0, journal, transformer, snapshot);

			Because of = () =>
				handler.OnNext(new TransformationItem(), 1, false);

			It should_skip_that_message = () =>
				transformer.Received(0).Handle(Arg.Any<object>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<long>());
		}

		public class when_the_message_is_handled_during_replay
		{
			public class when_the_message_yields_NO_resulting_messages
			{
				Establish context = () =>
				{
					transformer.Handle(item.Body, item.Headers, ReplayMessageSequence).Returns(new object[0]);
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.MessageSequence = ReplayMessageSequence;
					item.Deserialize(new JsonSerializer());
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_anything_else = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_one = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_assign_the_message_sequence_on_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence);
			}
			
			public class when_the_message_yeilds_a_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 1;
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, ReplayMessageSequence).Returns(new object[] { "hello", "world" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence).Returns(new object[0]);
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_reassign_the_sequence_number_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);
			}

			public class when_the_yielded_messages_yield_more_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 1;
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, ReplayMessageSequence).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence + 1).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence + 1).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_publish_the_incoming_message_and_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_reassign_the_message_sequence_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);
			}

			public class when_a_subsequent_message_is_received
			{
				Establish context = () =>
				{
					var serializer = new JsonSerializer();
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.MessageSequence = JournaledSequence - 2;
					item.Deserialize(serializer);
					item2 = new TransformationItem();
					item2.AsForeignMessage(Encoding.UTF8.GetBytes("2"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item2.MessageSequence = JournaledSequence - 1;
					item2.Deserialize(serializer);

					transformer.Handle(item.Body, item.Headers, ReplayMessageSequence).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), ReplayMessageSequence).Returns(new object[0]);
					transformer.Handle(item2.Body, item2.Headers, ReplayMessageSequence).Returns(new object[0]);
					
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
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
			public class when_the_message_yields_NO_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveMessageSequence).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
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
					snapshot.Received().Track(JournaledSequence + 1);

				It should_assign_the_message_sequence_on_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_the_message_yields_one_or_more_resulting_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveMessageSequence).Returns(new object[] { "hello", "world" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence).Returns(new object[0]);
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
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
							SerializedType = string.Empty.ResolvableTypeName(),
							Headers = new Dictionary<string, string>()
						},
						new JournalItem
						{
							Body = "world",
							MessageSequence = JournaledSequence + 3, 
							ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
							SerializedType = string.Empty.ResolvableTypeName(),
							Headers = new Dictionary<string, string>()
						}
					});

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Track(JournaledSequence + 3);

				It should_assign_the_message_sequence_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_the_yielded_messages_yield_more_messages
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.Deserialize(new JsonSerializer());
					transformer.Handle(item.Body, item.Headers, LiveMessageSequence).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence + 1).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence + 1).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
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
							SerializedType = string.Empty.ResolvableTypeName(),
							Headers = new Dictionary<string, string>()
						},
						new JournalItem
						{
							Body = "world",
							MessageSequence = JournaledSequence + 3, 
							ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
							SerializedType = string.Empty.ResolvableTypeName(),
							Headers = new Dictionary<string, string>()
						}
					});

				It should_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.Received().Track(JournaledSequence + 3);

				It should_assign_the_message_sequence = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence + 1);
			}

			public class when_a_subsequent_message_is_received
			{
				Establish context = () =>
				{
					var serializer = new JsonSerializer();
					item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item.Deserialize(serializer);
					item2 = new TransformationItem();
					item2.AsForeignMessage(Encoding.UTF8.GetBytes("2"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
					item2.Deserialize(serializer);

					transformer.Handle(item.Body, item.Headers, LiveMessageSequence).Returns(new object[] { "hello" });
					transformer.Handle("hello", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence + 1).Returns(new object[] { "world" });
					transformer.Handle("world", Arg.Any<Dictionary<string, string>>(), LiveMessageSequence + 2).Returns(new object[0]);
					transformer.Handle(item2.Body, item.Headers, LiveMessageSequence + 3).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
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
						SerializedType = string.Empty.ResolvableTypeName(),
						Headers = new Dictionary<string, string>()
					},
					new JournalItem
					{
						Body = "world",
						MessageSequence = JournaledSequence + 3,
						ItemActions = JournalItemAction.Dispatch | JournalItemAction.Journal,
						SerializedType = string.Empty.ResolvableTypeName(),
						Headers = new Dictionary<string, string>()
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

		public class when_transitioning_from_replay_to_the_live_stream
		{
			Establish context = () =>
			{
				transformer.Handle(item.Body, item.Headers, ReplayMessageSequence).Returns(new object[0]);
				item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
				item.MessageSequence = ReplayMessageSequence;
				item.Deserialize(new JsonSerializer());

				liveItem = new TransformationItem();
				liveItem.AsForeignMessage(Encoding.UTF8.GetBytes("2"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
				item.Deserialize(new JsonSerializer());

				handler = new TransformationHandler(JournaledSequence, journal, transformer, snapshot);
				handler.OnNext(item, 234234, false);
			};

			Because of = () =>
				handler.OnNext(liveItem, 234235, false);

			It should_wait_until_the_live_stream_to_begin_incrementing_the_sequence_number = () =>
			{
				item.MessageSequence.ShouldEqual(ReplayMessageSequence); // didn't change
				liveItem.MessageSequence.ShouldEqual(LiveMessageSequence); // was assigned
			};

			static TransformationItem liveItem;
		}
		
		Establish context = () =>
		{
			item = new TransformationItem();
			journal = new RingBufferHarness<JournalItem>();
			transformer = Substitute.For<ITransformer>();
			snapshot = Substitute.For<ISystemSnapshotTracker>();
		};

		const long JournaledSequence = 42;
		const long LiveMessageSequence = JournaledSequence + 1;
		const long ReplayMessageSequence = JournaledSequence;
		static TransformationItem item;
		static TransformationHandler handler;
		static RingBufferHarness<JournalItem> journal;
		static ITransformer transformer;
		static ISystemSnapshotTracker snapshot;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
