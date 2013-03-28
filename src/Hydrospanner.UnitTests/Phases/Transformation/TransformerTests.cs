#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using NSubstitute;
	using Snapshot;

	[Subject(typeof(Transformer))]
	public class when_initializing_the_transformer
	{
		It should_throw_if_the_repository_is_null = () =>
			Try(() => new Transformer(null, new RingBufferHarness<SnapshotItem>(), 42)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_ring_is_null = () =>
			Try(() => new Transformer(Substitute.For<IRepository>(), null, 42)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_journaled_sequence_is_out_of_range = () =>
		{
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), -1)).ShouldBeOfType<ArgumentOutOfRangeException>();
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), long.MinValue)).ShouldBeOfType<ArgumentOutOfRangeException>();
		};

		It should_NOT_throw_if_the_parameters_are_appropriate = () =>
		{
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), 1)).ShouldBeNull();
			Try(() => new Transformer(Substitute.For<IRepository>(), new RingBufferHarness<SnapshotItem>(), long.MaxValue)).ShouldBeNull();
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
				hydratable = new TestHydratable(!IsPublicSnapshot, !BecomesComplete, Key);
				repository.Load(Incoming, Headers).Returns(new[] { hydratable });
			};

			Because of = () =>
				result = transformer.Handle(Incoming, Headers, IsLiveMessage).ToList();

			It should_transform_the_hydratable_with_the_incoming_message_and_return_the_resulting_messages = () =>
				result.Single().ShouldBeLike(Incoming);
		}

		public class when_handling_a_replay_message
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(!IsPublicSnapshot, !BecomesComplete, Key);
				repository.Load(Incoming, Headers).Returns(new[] { hydratable });
			};

			Because of = () =>
				result = transformer.Handle(Incoming, Headers, !IsLiveMessage).ToList();

			It should_NOT_gather_and_return_any_messages = () =>
				result.ShouldBeEmpty();
		}

		public class when_the_hydratable_is_a_public_snapshot
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(IsPublicSnapshot, !BecomesComplete, Key);
				repository.Load(Incoming, Headers).Returns(new[] { hydratable });
			};

			Because of = () =>
				transformer.Handle(Incoming, Headers, !IsLiveMessage);

			It should_take_a_snapshot = () =>
				snapshotRing.AllItems.Single().ShouldBeLike(new SnapshotItem
				{
					CurrentSequence = 43,
					IsPublicSnapshot = true,
					Key = hydratable.Key,
					Memento = new SomethingHappenedProjection { Value = Incoming.Value },
					MementosRemaining = 0,
					Serialized = null
				});
		}

		public class when_the_hydratable_becomes_complete_as_a_result_of_transformation
		{
			Establish context = () =>
			{
				hydratable = new TestHydratable(!IsPublicSnapshot, BecomesComplete, Key);
				repository.Load(Incoming, Headers).Returns(new[] { hydratable });
			};

			Because of = () =>
				transformer.Handle(Incoming, Headers, IsLiveMessage);

			It should_take_a_snapshot = () =>
				snapshotRing.AllItems.Single().ShouldBeLike(new SnapshotItem
				{
					CurrentSequence = 43,
					IsPublicSnapshot = true,
					Key = hydratable.Key,
					Memento = new SomethingHappenedProjection { Value = Incoming.Value },
					MementosRemaining = 0,
					Serialized = null
				});

			It should_delete_the_hydratable = () =>
				repository.Received().Delete(hydratable);
		}

		public class when_handling_a_subsequent_message_that_corresponds_to_a_public_snapshot
		{
			Establish context = () =>
			{
				subsequentIncoming = new SomethingHappenedEvent { Value = "Goodbye, World!" };
				hydratable = new TestHydratable(IsPublicSnapshot, !BecomesComplete, Key);
				repository.Load(Incoming, Headers).Returns(new[] { hydratable });
				repository.Load(subsequentIncoming, Headers).Returns(new[] { hydratable });
				transformer.Handle(Incoming, Headers, IsLiveMessage);
			};

			Because of = () =>
				transformer.Handle(subsequentIncoming, Headers, IsLiveMessage);

			It should_keep_track_of_the_message_sequence_on_the_snapshot = () =>
				snapshotRing.AllItems.ShouldBeLike(new[]
				{
					new SnapshotItem
					{
						CurrentSequence = 43,
						IsPublicSnapshot = true,
						Key = hydratable.Key,
						Memento = new SomethingHappenedProjection { Value = Incoming.Value },
						MementosRemaining = 0,
						Serialized = null
					},
					new SnapshotItem
					{
						CurrentSequence = 44,
						IsPublicSnapshot = true,
						Key = hydratable.Key,
						Memento = new SomethingHappenedProjection { Value = subsequentIncoming.Value },
						MementosRemaining = 0,
						Serialized = null
					}
				});

			static SomethingHappenedEvent subsequentIncoming;
		}

		Establish context = () =>
		{
			repository = Substitute.For<IRepository>();
			snapshotRing = new RingBufferHarness<SnapshotItem>();
			transformer = new Transformer(repository, snapshotRing, 42);
		};

		const bool IsPublicSnapshot = true;
		const bool BecomesComplete = true;
		const bool IsLiveMessage = true;
		const string Key = "Key";
		static readonly Dictionary<string, string> Headers = null;
		static readonly SomethingHappenedEvent Incoming = new SomethingHappenedEvent { Value = "Hello, World!" };
		static TestHydratable hydratable;
		static List<object> result; 
		static Transformer transformer;
		static IRepository repository;
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

	public class TestHydratable : IHydratable, IHydratable<SomethingHappenedEvent>
	{
		public readonly List<string> EventsReceived = new List<string>();
		public readonly List<string> EventsPublished = new List<string>();

		public string Key { get { return this.key; } }
		public bool IsComplete { get; private set; }
		public bool IsPublicSnapshot { get { return this.isPublicSnapshot; } }

		public IEnumerable<object> GatherMessages()
		{
			var message = this.EventsReceived.Last();
			this.EventsPublished.Add(message);
			yield return new SomethingHappenedEvent { Value = message };
		}

		public object GetMemento()
		{
			return new SomethingHappenedProjection { Value = this.EventsReceived.Last() };
		}

		public void Hydrate(SomethingHappenedEvent message, Dictionary<string, string> headers, bool live)
		{
			this.EventsReceived.Add(message.Value);

			if (this.becomesComplete)
				this.IsComplete = true;
		}

		public TestHydratable(bool isPublicSnapshot, bool becomesComplete, string key)
		{
			this.isPublicSnapshot = isPublicSnapshot;
			this.becomesComplete = becomesComplete;
			this.key = key;
		}
		
		private readonly bool isPublicSnapshot;
		private readonly bool becomesComplete;
		private readonly string key;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
