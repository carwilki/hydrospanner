#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using NSubstitute;
	using Snapshot;
	using Timeout;

	[Subject(typeof(Transformer))]
	public class when_initializing_the_transformer
	{
		It should_throw_if_the_repository_is_null = () =>
			Try(() => new Transformer(null, new RingBufferHarness<SnapshotItem>(), Substitute.For<ITimeoutWatcher>())).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_ring_is_null = () =>
			Try(() => new Transformer(Substitute.For<IRepository>(), null, Substitute.For<ITimeoutWatcher>())).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_watcher_is_null = () =>
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), null)).ShouldBeOfType<ArgumentNullException>();

		It should_NOT_throw_if_the_parameters_are_appropriate = () =>
		{
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), Substitute.For<ITimeoutWatcher>())).ShouldBeNull();
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), Substitute.For<ITimeoutWatcher>())).ShouldBeNull();
		};

		static Exception Try(Action action)
		{
			return Catch.Exception(action);
		}
	}

	[Subject(typeof(Transformer))]
	public class when_transforming_hydratables
	{
		public class when_handling_a_live_message
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(!BecomesComplete, Key);
				repository.Load(liveDelivery).Returns(new[] { hydratable });
				watcher.Filter(hydratable.Key, Incoming).Returns(Incoming);
			};

			Because of = () =>
				result = transformer.Transform(liveDelivery).ToList();

			It should_transform_the_hydratable_with_the_incoming_message_and_return_the_resulting_messages = () =>
				result.Single().ShouldBeLike(Incoming);

			It should_filter_all_messages_through_the_timeout_watcher_for_potential_transformation = () =>
				watcher.Received(1);
		}

		public class when_handling_a_replay_message
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(!BecomesComplete, Key);
				repository.Load(replayDelivery).Returns(new[] { hydratable });
			};

			Because of = () =>
				result = transformer.Transform(replayDelivery).ToList();

			It should_try_to_clear_any_messages_from_the_hydratable = () =>
				hydratable.PendingMessages.ShouldBeEmpty();

			It should_NOT_gather_and_return_any_messages = () =>
				result.ShouldBeEmpty();
		}

		public class when_the_hydratable_is_a_public_snapshot
		{
			Establish context = () =>
			{
				hydratable = new PublicHydratable(Key);
				repository.Load(liveDelivery).Returns(new[] { hydratable });
			};

			Because of = () =>
				transformer.Transform(liveDelivery);

			It should_take_a_snapshot = () =>
				snapshotRing.AllItems.Single().ShouldBeLike(new SnapshotItem
				{
					CurrentSequence = LiveMessageSequence,
					IsPublicSnapshot = true,
					Key = hydratable.Key,
					Memento = new SomethingHappenedProjection { Value = Incoming.Value },
					MementoType = typeof(SomethingHappenedProjection).ResolvableTypeName(),
					MementosRemaining = 0,
					Serialized = null
				});
		}

		public class when_the_public_snapshot_hydratable_is_being_replayed
		{
			Establish context = () =>
			{
				hydratable = new PublicHydratable(Key);
				repository.Load(replayDelivery).Returns(new[] { hydratable });
			};

			Because of = () =>
				transformer.Transform(replayDelivery);

			It should_NOT_take_a_snapshot = () =>
				snapshotRing.AllItems.ShouldBeEmpty();
		}
		
		public class when_the_hydratable_is_not_a_public_snapshot
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(BecomesComplete, Key);
				repository.Load(liveDelivery).Returns(new[] { hydratable });
			};

			Because of = () =>
				transformer.Transform(liveDelivery);

			It should_NOT_take_a_snapshot = () =>
				snapshotRing.AllItems.ShouldBeEmpty();
		}

		public class when_the_hydratable_collection_cannot_be_modified
		{
			Establish context = () =>
			{
				hydratable = new ReadOnlyMessagesHydratable();
				repository.Load(liveDelivery).Returns(new[] { hydratable });
			};

			Because of = () =>
				thrown = Catch.Exception(() => transformer.Transform(liveDelivery));

			It should_not_attempt_to_clear_the_collection = () =>
				thrown.ShouldBeNull();

			static Exception thrown;
		}

		public class when_the_public_hydratable_becomes_complete_as_a_result_of_transformation
		{
			Establish context = () =>
			{
				hydratable = new PublicHydratable(Key, BecomesComplete);
				repository.Load(liveDelivery).Returns(new[] { hydratable });

				timeoutHydratable = Substitute.For<IHydratable>();
				timeoutHydratable.PendingMessages.Returns(x => new[] { message });

				watcher.Filter(Arg.Any<string>(), Arg.Any<object>()).Returns(x => x.Args()[1]);
				watcher.Abort(hydratable).Returns(x => timeoutHydratable);
			};

			Because of = () =>
				result = transformer.Transform(liveDelivery).ToList();

			It should_take_a_snapshot = () =>
				snapshotRing.AllItems.Single().ShouldBeLike(new SnapshotItem
				{
					CurrentSequence = 43,
					IsPublicSnapshot = true,
					Key = hydratable.Key,
					Memento = new SomethingHappenedProjection { Value = Incoming.Value },
					MementoType = typeof(SomethingHappenedProjection).ResolvableTypeName(),
					MementosRemaining = 0,
					Serialized = null
				});

			It should_delete_the_hydratable = () =>
				repository.Received().Delete(hydratable);

			It should_abort_any_corresponding_timeouts_from_the_watcher = () =>
				result.Last().ShouldEqual(message);

			static IHydratable timeoutHydratable;
			static readonly object message = new TimeoutAbortedEvent(string.Empty, SystemTime.UtcNow);
		}

		public class when_handling_a_subsequent_message_that_corresponds_to_a_public_snapshot
		{
			Establish context = () =>
			{
				subsequentIncoming = new SomethingHappenedEvent { Value = "Goodbye, World!" };
				subsequentDelivery = new Delivery<SomethingHappenedEvent>(subsequentIncoming, Headers, LiveMessageSequence + 1, true, true);
				hydratable = new PublicHydratable(Key);
				repository.Load(liveDelivery).Returns(new[] { hydratable });
				repository.Load(subsequentDelivery).Returns(new[] { hydratable });

				transformer.Transform(liveDelivery);
			};

			Because of = () =>
				transformer.Transform(subsequentDelivery);

			It should_keep_track_of_the_message_sequence_on_the_snapshot = () =>
				snapshotRing.AllItems.ShouldBeLike(new[]
				{
					new SnapshotItem
					{
						CurrentSequence = 43,
						IsPublicSnapshot = true,
						Key = hydratable.Key,
						Memento = new SomethingHappenedProjection { Value = Incoming.Value },
						MementoType = typeof(SomethingHappenedProjection).ResolvableTypeName(),
						MementosRemaining = 0,
						Serialized = null
					},
					new SnapshotItem
					{
						CurrentSequence = 44,
						IsPublicSnapshot = true,
						Key = hydratable.Key,
						Memento = new SomethingHappenedProjection { Value = subsequentIncoming.Value },
						MementoType = typeof(SomethingHappenedProjection).ResolvableTypeName(),
						MementosRemaining = 0,
						Serialized = null
					}
				});

			static SomethingHappenedEvent subsequentIncoming;
			static Delivery<SomethingHappenedEvent> subsequentDelivery;
		}

		Establish context = () =>
		{
			repository = Substitute.For<IRepository>();
			snapshotRing = new RingBufferHarness<SnapshotItem>();
			watcher = Substitute.For<ITimeoutWatcher>();
			transformer = new Transformer(repository, snapshotRing, watcher);
			replayDelivery = new Delivery<SomethingHappenedEvent>(Incoming, Headers, ReplayMessage, false, true);
			liveDelivery = new Delivery<SomethingHappenedEvent>(Incoming, Headers, LiveMessageSequence, true, true); 
		};

		const long JournaledSequence = 42;
		const long LiveMessageSequence = JournaledSequence + 1;
		const long ReplayMessage = JournaledSequence - 1;
		const bool IsPublicSnapshot = true;
		const bool BecomesComplete = true;
		const string Key = "Key";
		static readonly Dictionary<string, string> Headers = null;
		static readonly SomethingHappenedEvent Incoming = new SomethingHappenedEvent { Value = "Hello, World!" };
		static Delivery<SomethingHappenedEvent> replayDelivery;
		static Delivery<SomethingHappenedEvent> liveDelivery;
		static TestHydratable hydratable;
		static List<object> result; 
		static Transformer transformer;
		static IRepository repository;
		static ITimeoutWatcher watcher;
		static RingBufferHarness<SnapshotItem> snapshotRing;
	}

	public class SomethingHappenedProjection
	{
		public string Value { get; set; }
	}

	public class SomethingHappenedEvent
	{
		public string Value { get; set; }
	}

	public class TestHydratable : IHydratable<SomethingHappenedEvent>
	{
		public readonly List<string> EventsReceived = new List<string>();

		public string Key { get { return this.key; } }
		public bool IsComplete { get; private set; }
		public virtual ICollection<object> PendingMessages { get; private set; }
		public object Memento { get { return this.memento ?? new SomethingHappenedProjection { Value = this.EventsReceived.Last() }; } }

		public virtual void Hydrate(Delivery<SomethingHappenedEvent> delivery)
		{
			this.EventsReceived.Add(delivery.Message.Value);
			this.PendingMessages.Add(delivery.Message);

			if (this.becomesComplete)
				this.IsComplete = true;
		}

		public TestHydratable(bool becomesComplete, string key, object memento = null)
		{
			this.becomesComplete = becomesComplete;
			this.key = key;
			this.memento = memento;
			this.PendingMessages = new List<object>();
		}
		
		private readonly bool becomesComplete;
		private readonly string key;
		private readonly object memento;
	}

	public class PublicHydratable : TestHydratable, IPublicHydratable
	{
		public PublicHydratable(string key, bool becomesComplete = false)
			: base(becomesComplete, key, null)
		{
		}

		public Type MementoType
		{
			get { return typeof(string); }
		}
	}

	public class ReadOnlyMessagesHydratable : TestHydratable
	{
		public ReadOnlyMessagesHydratable() : base(false, string.Empty, null)
		{
		}

		public override ICollection<object> PendingMessages
		{
			get { return new object[] { 0, 1, 2, 3 }; }
		}
		public override void Hydrate(Delivery<SomethingHappenedEvent> delivery)
		{
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
