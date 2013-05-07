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
				Try(() => new TransformationHandler(-1, journal, deliveryHandler, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
				Try(() => new TransformationHandler(long.MinValue, journal, deliveryHandler, snapshot)).ShouldBeOfType<ArgumentOutOfRangeException>();
			};

			It should_throw_if_the_journal_is_null = () =>
			{
				Try(() => new TransformationHandler(0, null, deliveryHandler, snapshot)).ShouldBeOfType<ArgumentNullException>();
				Try(() => new TransformationHandler(1, null, deliveryHandler, snapshot)).ShouldBeOfType<ArgumentNullException>();
				Try(() => new TransformationHandler(int.MaxValue, null, deliveryHandler, snapshot)).ShouldBeOfType<ArgumentNullException>();
			};

			It should_throw_if_the_transformer_is_null = () =>
				Try(() => new TransformationHandler(1, journal, null, snapshot)).ShouldBeOfType<ArgumentNullException>();

			It should_throw_if_the_snapshot_is_null = () =>
				Try(() => new TransformationHandler(1, journal, deliveryHandler, null)).ShouldBeOfType<ArgumentNullException>();

			static Exception Try(Action action)
			{
				return Catch.Exception(action);
			}
		}

		public class when_a_live_message_with_no_body_arrives
		{
			Establish context = () =>
				handler = new TransformationHandler(0, journal, deliveryHandler, snapshot);

			Because of = () =>
				handler.OnNext(new TransformationItem(), 1, false);

			It should_skip_that_message = () =>
				deliveryHandler.Received(0).Deliver(Arg.Any<object>(), Arg.Any<long>());
		}

		public class when_a_live_message_arrives_after_a_message_with_no_body
		{
			Establish context = () =>
			{
				handler = new TransformationHandler(0, journal, deliveryHandler, snapshot);
				handler.OnNext(new TransformationItem(), 1, false);
			};

			Because of = () =>
				handler.OnNext(subsequentItem, 2, false);

			It should_process_the_subsequent_message = () =>
				deliveryHandler.Received(1).Deliver(subsequentItem, true);

			static readonly TransformationItem subsequentItem = new TransformationItem
			{
				Body = new object()
			};
		}

		public class when_a_journaled_message_with_no_body_arrives
		{
			Establish context = () =>
				handler = new TransformationHandler(0, journal, deliveryHandler, snapshot);

			Because of = () =>
			{
				handler.OnNext(serializationFailure, 1, false);
				handler.OnNext(subsequentItem, 2, false);
			};

			It should_skip_all_messages_thereafter = () =>
				deliveryHandler.Received(0).Deliver(Arg.Any<object>(), Arg.Any<long>());

			static readonly TransformationItem serializationFailure = new TransformationItem
			{
				MessageSequence = 1,
			};
			static readonly TransformationItem subsequentItem = new TransformationItem
			{
				MessageSequence = 2,
				Body = new object()
			};
		}

		public class when_the_message_is_handled_during_replay
		{
			public class when_the_message_yields_NO_resulting_messages
			{
				Establish context = () =>
				{
					item = new TransformationItem
					{
						Body = 1,
						SerializedBody = Encoding.UTF8.GetBytes("1"),
						SerializedType = default(int).ResolvableTypeName(),
						Headers = new Dictionary<string, string>(),
						SerializedHeaders = Encoding.UTF8.GetBytes("{}"),
						MessageSequence = ReplayMessageSequence
					};
					deliveryHandler.Deliver(item, true).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_anything_else = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot_by_one = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_assign_the_message_sequence_on_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(ReplayMessageSequence);
			}
			
			public class when_the_message_yields_a_resulting_messages
			{
				Establish context = () =>
				{
					item = new TransformationItem
					{
						Body = 1,
						SerializedBody = Encoding.UTF8.GetBytes("1"),
						SerializedType = default(int).ResolvableTypeName(),
						Headers = new Dictionary<string, string>(),
						SerializedHeaders = Encoding.UTF8.GetBytes("{}"),
						MessageSequence = JournaledSequence - 1
					};

					deliveryHandler
						.Deliver(item, false)
						.Returns(new object[] { "hello", "world" });
					deliveryHandler
						.Deliver("hello", JournaledSequence)
						.Returns(new object[0]);
					deliveryHandler
						.Deliver("world", JournaledSequence + 1)
						.Returns(new object[0]);
					
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_or_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_increment_the_snapshot = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_reassign_the_sequence_number_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 1);

				It should_have_handled_the_correct_transformation_messages = () =>
					deliveryHandler.Received(1).Deliver(item, false);

				It should_have_handled_the_correct_resulting_messages = () =>
					deliveryHandler.Received(0).Deliver(Arg.Any<string>(), Arg.Any<long>());
			}

			public class when_the_yielded_messages_yield_more_messages
			{
				Establish context = () =>
				{
					item = new TransformationItem
					{
						Body = 1,
						SerializedBody = Encoding.UTF8.GetBytes("1"),
						SerializedType = default(int).ResolvableTypeName(),
						Headers = new Dictionary<string, string>(),
						SerializedHeaders = Encoding.UTF8.GetBytes("{}"),
						MessageSequence = JournaledSequence - 10
					};
					
					deliveryHandler
						.Deliver(item, false)
						.Returns(new object[] { "hello" });
					deliveryHandler
						.Deliver("hello", ReplayMessageSequence - 9)
						.Returns(new object[] { "world" });

					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				};

				Because of = () =>
					handler.OnNext(item, 1, false);

				It should_NOT_publish_the_incoming_message_and_the_resulting_messages = () =>
					journal.AllItems.ShouldBeEmpty();

				It should_NOT_increment_the_snapshot_by_the_number_of_messages_published = () =>
					snapshot.DidNotReceive().Track(Arg.Any<long>());

				It should_NOT_reassign_the_message_sequence_of_the_incoming_message = () =>
					item.MessageSequence.ShouldEqual(JournaledSequence - 10);

				It should_have_handled_the_correct_messages = () =>
				{
					// sanity check
					deliveryHandler.Received(1).Deliver(item, Arg.Any<bool>());
					deliveryHandler.Received(0).Deliver(Arg.Any<string>(), Arg.Any<long>());
				};
			}

			public class when_a_subsequent_message_is_received
			{
				Establish context = () =>
				{
					item = new TransformationItem
					{
						Body = 1,
						SerializedBody = Encoding.UTF8.GetBytes("1"),
						Headers = new Dictionary<string, string>(),
						SerializedHeaders = Encoding.UTF8.GetBytes("{}"),
						SerializedType = default(int).ResolvableTypeName(),
						MessageSequence = JournaledSequence - 5,
					};
					item2 = new TransformationItem
					{
						Body = 2,
						SerializedBody = Encoding.UTF8.GetBytes("1"),
						Headers = new Dictionary<string, string>(),
						SerializedHeaders = Encoding.UTF8.GetBytes("{}"),
						SerializedType = default(int).ResolvableTypeName(),
						MessageSequence = JournaledSequence - 2
					};

					deliveryHandler.Deliver(item, false).Returns(new object[] { "hello" });
					deliveryHandler.Deliver("hello", JournaledSequence - 4).Returns(new object[] { "world" });
					deliveryHandler.Deliver("world", JournaledSequence - 3).Returns(new object[0]);
					deliveryHandler.Deliver(item2, true).Returns(new object[0]);
					
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
					handler.OnNext(item, 1, false);
				};

				Because of = () =>
					handler.OnNext(item2, 2, false);

				It should_track_the_sequence_number_correctly = () => 
					journal.AllItems.ShouldBeEmpty();

				It should_NOT_reassign_the_message_sequences_for_the_incoming_messages = () =>
				{
					item.MessageSequence.ShouldEqual(JournaledSequence - 5);
					item2.MessageSequence.ShouldEqual(JournaledSequence - 2);
				};

				It should_have_handled_the_correct_messages = () =>
				{
					// sanity check
					deliveryHandler.Received(1).Deliver(item, Arg.Any<bool>());
					deliveryHandler.Received(0).Deliver("hello", Arg.Any<long>());
					deliveryHandler.Received(0).Deliver("world", Arg.Any<long>());
					deliveryHandler.Received(1).Deliver(item2, Arg.Any<bool>());
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
					deliveryHandler.Deliver(item, true).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
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
					deliveryHandler.Deliver(item, true).Returns(new object[] { "hello", "world" });
					deliveryHandler.Deliver("hello", LiveMessageSequence).Returns(new object[0]);
					deliveryHandler.Deliver("world", LiveMessageSequence).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
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

				It should_correctly_sequence_the_messages_for_delivery_to_the_application = () =>
				{
					deliveryHandler.Received(1).Deliver("hello", JournaledSequence + 2);
					deliveryHandler.Received(1).Deliver("world", JournaledSequence + 3);
				};

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
					deliveryHandler.Deliver(item, true).Returns(new object[] { "hello" });
					deliveryHandler.Deliver("hello", LiveMessageSequence + 1).Returns(new object[] { "world" });
					deliveryHandler.Deliver("world", LiveMessageSequence + 1).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
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

					deliveryHandler.Deliver(item, true).Returns(new object[] { "hello" });
					deliveryHandler.Deliver("hello", LiveMessageSequence + 1).Returns(new object[] { "world" });
					deliveryHandler.Deliver("world", LiveMessageSequence + 2).Returns(new object[0]);
					deliveryHandler.Deliver(item2, true).Returns(new object[0]);
					handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
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
				deliveryHandler.Deliver(item, true).Returns(new object[0]);
				item.AsForeignMessage(Encoding.UTF8.GetBytes("1"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
				item.MessageSequence = ReplayMessageSequence;
				item.Deserialize(new JsonSerializer());

				liveItem = new TransformationItem();
				liveItem.AsForeignMessage(Encoding.UTF8.GetBytes("2"), default(int).ResolvableTypeName(), null, Guid.NewGuid(), null);
				liveItem.Deserialize(new JsonSerializer());

				handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
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

		public class when_a_transformation_item_is_purely_transient
		{
			Establish context = () =>
			{
				handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				deliveryHandler.Deliver(Arg.Do<TransformationItem>(x => receivedSequence = x.MessageSequence), true);
				transientItem = new TransformationItem();
				transientItem.AsTransientMessage(new object());
			};

			Because of = () =>
				handler.OnNext(transientItem, 1, true);

			It should_not_increment_the_sequence_for_transient_messages = () =>
				receivedSequence.ShouldEqual(JournaledSequence);

			It should_forwarded_the_transient_message_journal_ring_for_acknowledgment_only = () =>
				journal.AllItems[0].ItemActions.ShouldEqual(JournalItemAction.Acknowledge);

			static TransformationItem transientItem;
			static long receivedSequence;
		}

		public class when_a_transient_message_causes_other_messages_to_be_generated
		{
			Establish context = () =>
			{
				handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				transientItem = new TransformationItem();
				transientItem.AsTransientMessage(new object());
				deliveryHandler.Deliver(transientItem, true).Returns(new object[] { "hello", "world" });
			};

			Because of = () =>
				handler.OnNext(transientItem, 1, true);

			It should_correctly_sequence_the_resulting_messages_for_delivery_to_the_application = () =>
			{
				deliveryHandler.Received(1).Deliver("hello", JournaledSequence + 1);
				deliveryHandler.Received(1).Deliver("world", JournaledSequence + 2);
			};

			It should_push_the_incoming_and_generated_messages_to_the_journal_queue = () =>
				journal.AllItems.ShouldBeLike(new[]
				{
					new JournalItem
					{
						MessageSequence = 0,
						Body = transientItem.Body,
						ForeignId = transientItem.ForeignId,
						Headers = transientItem.Headers,
						SerializedBody = null,
						SerializedType = null,
						SerializedHeaders = null,
						Acknowledgment = null,
						ItemActions = JournalItemAction.Acknowledge
					},
					Create("hello", JournaledSequence + 1),
					Create("world", JournaledSequence + 2)
				});

			static JournalItem Create(string body, long sequence)
			{
				var item = new JournalItem();
				item.AsTransformationResultMessage(sequence, body, new Dictionary<string, string>());
				return item;
			}

			static TransformationItem transientItem;
		}

		public class when_subsequent_messages_arrive_after_a_transient_message_generates_more_messages
		{
			Establish context = () =>
			{
				handler = new TransformationHandler(JournaledSequence, journal, deliveryHandler, snapshot);
				transientItem = new TransformationItem();
				transientItem.AsTransientMessage(new object());

				subsequentItem = new TransformationItem();
				subsequentItem.AsForeignMessage(null, null, new Dictionary<string, string>(), Guid.NewGuid(), null);
				subsequentItem.Body = new object();
				deliveryHandler.Deliver(transientItem, true).Returns(transientGenerated);

				handler.OnNext(transientItem, 1, true);
			};

			Because of = () =>
				handler.OnNext(subsequentItem, 2, true);

			It should_correctly_sequence_the_subsequent_message_for_delivery_to_the_application = () =>
				subsequentItem.MessageSequence.ShouldEqual(JournaledSequence + transientGenerated.Length + 1);

			static TransformationItem transientItem;
			static TransformationItem subsequentItem;
			static readonly object[] transientGenerated = new object[] { "hello", "world" };
		}
		
		Establish context = () =>
		{
			item = new TransformationItem();
			journal = new RingBufferHarness<JournalItem>();
			deliveryHandler = Substitute.For<IDeliveryHandler>();
			snapshot = Substitute.For<ISystemSnapshotTracker>();
		};

		const long JournaledSequence = 42;
		const long LiveMessageSequence = JournaledSequence + 1;
		const long ReplayMessageSequence = JournaledSequence;
		static TransformationItem item;
		static TransformationHandler handler;
		static RingBufferHarness<JournalItem> journal;
		static IDeliveryHandler deliveryHandler;
		static ISystemSnapshotTracker snapshot;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
