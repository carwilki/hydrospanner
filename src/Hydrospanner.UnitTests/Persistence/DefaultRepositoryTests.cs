﻿#pragma warning disable 169, 414
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
				nullFactoryDelivery = new Delivery<long>(0, Headers, 1, true);
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

		public class when_taking_a_snapshot_and_restoring_the_repository_from_the_snapshot
		{
			Establish context = () =>
			{
				tombstoneDelivery = new Delivery<string>(Tombstone, Headers, 1, true);
				tombstoned = new MyHydratable(Tombstone);
				tombstoneInfo = new HydrationInfo(Tombstone, () => tombstoned);
				routes.Lookup(tombstoneDelivery).Returns(new[] { tombstoneInfo });
				routes.Restore(Document).Returns(Document);

				repository.Load(delivery).ToList();
				repository.Load(tombstoneDelivery).ToList();
				repository.Delete(tombstoned);
			};
			
			Because of = () =>
			{
				snapshot = repository.GetMementos().ToList();
				
				var restored = new DefaultRepository(routes);
				foreach (var memento in snapshot)
					restored.Restore(memento);

				snapshotOfRestoredRepository = restored.GetMementos().ToList();
			};

			It should_include_the_graveyard_first_in_the_snapshot = () =>
				snapshot.First().ShouldBeLike(new GraveyardMemento(new[] { Tombstone }));
			
			It should_include_the_rest_of_the_hydratables_after_the_graveyard_in_the_snapshot = () =>
				snapshot.Last().ShouldEqual(Document);

			It should_recreate_the_graveyard_state = () =>
				snapshotOfRestoredRepository.ShouldBeLike(snapshot);

			const string Tombstone = "Deleted";
			static HydrationInfo tombstoneInfo;
			static MyHydratable tombstoned;
			static List<object> snapshot;
			static List<object> snapshotOfRestoredRepository;
			static Delivery<string> tombstoneDelivery;
		}

		public class when_the_memento_returned_from_the_hydratable_is_null
		{
			Establish context = () =>
			{
				var info = new HydrationInfo("42", () => new NullMementoHydratable("42"));
				routes.Lookup(Arg.Any<Delivery<Guid>>()).Returns(new[] { info });
			};

			It should_add_them_to_the_repository_for_future_retreival = () =>
			{
				var loaded = repository.Load(new Delivery<Guid>()).ToList();
				loaded.Single().ShouldBeOfType<NullMementoHydratable>();
			};

			It should_not_yield_the_value_back_to_the_caller = () =>
				repository.GetMementos().ShouldBeEmpty();
		}

		Establish context = () =>
		{
			routes = Substitute.For<IRoutingTable>();
			repository = new DefaultRepository(routes);
			myHydrationInfo = new HydrationInfo(Key, () => Document);
			delivery = new Delivery<int>(Message, Headers, 1, true);

			routes.Lookup(delivery).Returns(new[] { myHydrationInfo });
		};

		const int Message = 42;
		static readonly string Key = Message.ToString(CultureInfo.InvariantCulture);
		static readonly MyHydratable Document = new MyHydratable(Key);
		static readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
		static HydrationInfo myHydrationInfo;
		static Delivery<int> delivery; 
		static IEnumerable<IHydratable> loadResult;
		static DefaultRepository repository;
		static IRoutingTable routes;
	}

	public class MyHydratable : IHydratable, IHydratable<int>
	{
		public string Key { get { return this.key; } }

		public object GetMemento()
		{
			return this;
		}

		#region -- Boilerplate --

		public bool IsComplete { get { return false; } }

		public bool IsPublicSnapshot { get { return false; } }

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

	public class NullMementoHydratable : IHydratable, IHydratable<Guid>
	{
		public string Key { get { return this.key; } }

		public object GetMemento()
		{
			return null;
		}

		#region -- Boilerplate --

		public bool IsComplete { get { return false; } }

		public bool IsPublicSnapshot { get { return false; } }

		public ICollection<object> PendingMessages
		{
			get { return null; }
		}

		public void Hydrate(Delivery<Guid> delivery)
		{
		}

		#endregion

		public NullMementoHydratable(string key)
		{
			this.key = key;
		}

		readonly string key;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414