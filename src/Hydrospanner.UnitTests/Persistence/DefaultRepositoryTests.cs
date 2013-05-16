#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Machine.Specifications;
	using NSubstitute;
	using Wireup;

	[Subject(typeof(DefaultRepository))]
	public class when_working_with_the_default_repository
	{
		public class when_loading_hydratables
		{
			public class when_the_hydratables_for_a_given_message_have_NOT_been_created
			{
				Because of = () =>
					loadResult = repository.Load(delivery).Cast<IHydratable>().ToList();

				It should_add_them_to_the_repository_for_future_retreival = () =>
					repository.Load(delivery).Single().ShouldEqual(Document);

				It should_return_the_newly_created_hydratables = () =>
					loadResult.Single().ShouldEqual(Document);
			}

			public class when_the_hydratable_has_been_tombstoned
			{
				Establish context = () =>
				{
					var created = repository.Load(delivery).Cast<IHydratable>().First();
					repository.Delete(created);
				};

				Because of = () =>
					loadResult = repository.Load(delivery).Cast<IHydratable>().ToList();

				It should_NOT_load_the_hydratable = () =>
					loadResult.ShouldBeEmpty();

				It should_NOT_create_a_new_hydratable = () =>
					repository.Load(delivery).ShouldBeEmpty();
			}
		}

		public class when_the_hydration_info_yields_a_null_hydratable
		{
			Establish context = () =>
			{
				nullFactoryDelivery = new Delivery<long>(0, Headers, 1, true, true);
				nullFactoryInfo = new HydrationInfo(NullFactory, () => null);
				routes.Lookup(nullFactoryDelivery).Returns(new[] { nullFactoryInfo });
			};

			Because of = () =>
				loadResult = repository.Load(nullFactoryDelivery).Cast<IHydratable>().ToList();

			It should_NOT_return_null_hydratables = () =>
				loadResult.ShouldBeEmpty();

			const string NullFactory = "NullFactory";
			static Delivery<long> nullFactoryDelivery;
			static HydrationInfo nullFactoryInfo;
		}

		public class when_the_hydration_info_yields_an_empty_key
		{
			Establish context = () =>
			{
				emptyKeyDelivery = new Delivery<long>(0, Headers, 1, true, true);
				emptyKeyInfo = new HydrationInfo(string.Empty, () => new MyHydratable(string.Empty));
				routes.Lookup(emptyKeyDelivery).Returns(new[] { emptyKeyInfo });
			};

			Because of = () =>
				loadResult = repository.Load(emptyKeyDelivery).Cast<IHydratable>().ToList();

			It should_NOT_return_hydratables_with_empty_keys = () =>
				loadResult.ShouldBeEmpty();

			static Delivery<long> emptyKeyDelivery;
			static HydrationInfo emptyKeyInfo;
		}

		public class when_taking_a_snapshot_and_restoring_the_repository_from_the_snapshot
		{
			Establish context = () =>
			{
				repository = new DefaultRepository(routes, graveyard);
				tombstoneDelivery = new Delivery<string>(Tombstone, Headers, 1, true, true);
				tombstoned = new MyHydratable(Tombstone);
				tombstoneInfo = new HydrationInfo(Tombstone, () => tombstoned);
				routes.Lookup(tombstoneDelivery).Returns(new[] { tombstoneInfo });
				routes.Restore(MyHydratable.MyMemento).Returns(Document);

				repository.Load(delivery).ToList();
				repository.Load(tombstoneDelivery).ToList();
				repository.Delete(tombstoned);
			};
			
			Because of = () =>
			{
				snapshot = repository.Items.Select(x => x.Memento).ToList();
				
				var restored = new DefaultRepository(routes, graveyard);
				foreach (var memento in snapshot)
					restored.Restore(memento);

				snapshotOfRestoredRepository = restored.Items.Select(x => x.Memento).ToList();
			};

			It should_include_the_graveyard_first_in_the_snapshot = () =>
				snapshot.First().ShouldBeLike(new GraveyardMemento(new[] { Tombstone }));
			
			It should_include_the_rest_of_the_hydratables_after_the_graveyard_in_the_snapshot = () =>
				snapshot.Last().ShouldEqual(MyHydratable.MyMemento);

			It should_recreate_the_graveyard_state = () =>
				snapshotOfRestoredRepository.ShouldBeLike(snapshot);

			const string Tombstone = "Deleted";
			static HydrationInfo tombstoneInfo;
			static MyHydratable tombstoned;
			static List<object> snapshot;
			static List<object> snapshotOfRestoredRepository;
			static Delivery<string> tombstoneDelivery;
		}

		public class when_deliverying_a_replay_message_against_a_public_hydratable
		{
			Establish context = () =>
				routes.Lookup(replayDelivery).Returns(x => new[] { publicInfo });

			Because of = () =>
				loadResult = repository.Load(replayDelivery).ToArray();

			It should_return_the_hydratable = () =>
				loadResult.Single().ShouldEqual(publicHydratable);

			It should_mark_the_hydratable_as_recently_accessed = () =>
				repository.Accessed.Single().ShouldEqual(new KeyValuePair<IHydratable, long>(publicHydratable, replayDelivery.Sequence));

			static readonly PublicHydratable publicHydratable = new PublicHydratable();
			static readonly Delivery<int> replayDelivery = new Delivery<int>(0, null, 13, false, true);
			static readonly HydrationInfo publicInfo = new HydrationInfo("key", () => publicHydratable);
		}

		public class when_deliverying_a_replay_message_against_a_private_hydratable
		{
			Establish context = () =>
				routes.Lookup(replayDelivery).Returns(x => new[] { privateInfo });

			Because of = () =>
				loadResult = repository.Load(replayDelivery).ToArray();

			It should_return_the_hydratable = () =>
				loadResult.Single().ShouldEqual(privateHydratable);

			It should_NOT_mark_the_hydratable_as_recently_accessed = () =>
				repository.Accessed.ShouldBeEmpty();

			static readonly MyHydratable privateHydratable = new MyHydratable("key");
			static readonly Delivery<int> replayDelivery = new Delivery<int>(0, null, 1, false, true);
			static readonly HydrationInfo privateInfo = new HydrationInfo("key", () => privateHydratable);
		}

		public class when_deliverying_a_live_message_against_a_public_hydratable
		{
			Establish context = () =>
				routes.Lookup(liveDelivery).Returns(x => new[] { publicInfo });

			Because of = () =>
				loadResult = repository.Load(liveDelivery).ToArray();

			It should_return_the_hydratable = () =>
				loadResult.Single().ShouldEqual(publicHydratable);

			It should_NOT_mark_the_hydratable_as_recently_accessed = () =>
				repository.Accessed.ShouldBeEmpty();

			static readonly PublicHydratable publicHydratable = new PublicHydratable();
			static readonly Delivery<int> liveDelivery = new Delivery<int>(0, null, 1, true, true);
			static readonly HydrationInfo publicInfo = new HydrationInfo("key", () => publicHydratable);
		}

		public class when_deleting_a_recently_accessed_public_hydratable
		{
			Establish context = () =>
			{
				routes.Lookup(replayDelivery).Returns(x => new[] { publicInfo });
				loadResult = repository.Load(replayDelivery).ToArray();
			};

			Because of = () =>
				repository.Delete(loadResult.Single());

			It should_remove_the_hydratable_from_the_recently_accessed_list = () =>
				repository.Accessed.ShouldBeEmpty();

			static readonly PublicHydratable publicHydratable = new PublicHydratable();
			static readonly Delivery<int> replayDelivery = new Delivery<int>(0, null, 1, false, true);
			static readonly HydrationInfo publicInfo = new HydrationInfo("key", () => publicHydratable);
		}

		public class when_restoring_a_graveyard_memento
		{
			Because of = () =>
				repository.Restore(memento);

			It should_populate_the_internal_graveyard_with_keys_from_the_memento = () =>
				graveyard.Contains(memento.Keys[0]).ShouldBeTrue();

			static readonly GraveyardMemento memento = new GraveyardMemento(new[] { "tombstoned-key" });
		}

		Establish context = () =>
		{
			graveyard = new HydratableGraveyard();
			routes = Substitute.For<IRoutingTable>();
			routes.Restore(Arg.Any<GraveyardMemento>()).Returns(graveyard);
			repository = new DefaultRepository(routes, graveyard);
			myHydrationInfo = new HydrationInfo(Key, () => Document);
			delivery = new Delivery<int>(Message, Headers, 1, true, true);

			routes.Lookup(delivery).Returns(new[] { myHydrationInfo });
		};

		const int Message = 42;
		static readonly string Key = Message.ToString(CultureInfo.InvariantCulture);
		static readonly MyHydratable Document = new MyHydratable(Key);
		static readonly PublicHydratable PublicDocument = new PublicHydratable();
		static readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
		static HydrationInfo myHydrationInfo;
		static Delivery<int> delivery;
		static IEnumerable<IHydratable> loadResult;
		static DefaultRepository repository;
		static IRoutingTable routes;
		static HydratableGraveyard graveyard;
	}

	public class MyHydratable : IHydratable<int>
	{
		public const int MyMemento = 4242;
		public string Key { get { return this.key; } }
		public object Memento { get { return MyMemento; } }
		public virtual Type MementoType { get { return typeof(int); } }
		public virtual bool IsPublicSnapshot { get { return false; } }

		#region -- Boilerplate --

		public bool IsComplete { get { return false; } }

		public ICollection<object> PendingMessages { get; private set; }

		public void Hydrate(Delivery<int> delivery)
		{
		}

		#endregion

		public MyHydratable(string key)
		{
			this.PendingMessages = new List<object>();
			this.key = key;
		}

		readonly string key;
	}

	public class PublicHydratable : MyHydratable
	{
		public override bool IsPublicSnapshot
		{
			get { return true; }
		}
		public override Type MementoType
		{
			get { return typeof(string); }
		}
		public PublicHydratable() : base("some-key")
		{
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
