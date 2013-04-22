#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence
{
	using Machine.Specifications;

	[Subject(typeof(HydratableGraveyard))]
	public class when_tracking_deleted_hydratables
	{
		It should_never_track_empty_keys_or_null_keys = () =>
		{
			store.Bury(string.Empty);
			store.Bury(string.Empty);
			store.Bury(string.Empty);
			store.Bury(null);
			store.Bury(null);
			store.Bury(null);

			store.Contains(string.Empty).ShouldBeFalse();
			store.Contains(null).ShouldBeFalse();
		};

		public class when_tracking_at_or_below_capacity
		{
			Because of = () =>
			{
				store.Bury(A);
				store.Bury(B);
				store.Bury(C);
				store.Bury(D);
			};

			It should_track_all_keys = () =>
			{
				store.Contains(A).ShouldBeTrue();
				store.Contains(B).ShouldBeTrue();
				store.Contains(C).ShouldBeTrue();
				store.Contains(D).ShouldBeTrue();
			};
		}

		public class when_tracking_above_capacity
		{
			Establish context = () =>
			{
				store.Bury(A);
				store.Bury(B);
				store.Bury(C);
				store.Bury(D);
			};

			Because of = () =>
				store.Bury("5"); // causes A to be forgotten

			It should_only_remember_the_newer_keys_within_capacity = () =>
			{
				store.Contains(B).ShouldBeTrue();
				store.Contains(C).ShouldBeTrue();
				store.Contains(D).ShouldBeTrue();
				store.Contains(A).ShouldBeFalse();
			};
		}

		Establish context = () =>
			store = new HydratableGraveyard(capacity: 4);

		const string A = "1";
		const string B = "2";
		const string C = "3";
		const string D = "4";
		static HydratableGraveyard store; 
	}

	[Subject(typeof(HydratableGraveyard))]
	public class when_retreiving_the_graveyard_memento
	{
		It should_capture_the_keys_in_special_object = () =>
		{
			var graveyard = new HydratableGraveyard();
			graveyard.Bury("1");
			graveyard.Bury("2");
			graveyard.Bury("3");
			var memento = graveyard.GetMemento();
			memento.ShouldBeLike(new GraveyardMemento(new[] { "1", "2", "3" }));
		};
	}

	[Subject(typeof(HydratableGraveyard))]
	public class when_restoring_the_graveyard_state_from_a_memento
	{
		Establish context = () =>
			memento = new GraveyardMemento(new[] { "1", "2", "3", "4" });

		Because of = () =>
			graveyard = new HydratableGraveyard(memento, 5);

		It should_add_all_items_in_the_memento_to_the_new_graveyard = () =>
		{
			graveyard.Contains("1").ShouldBeTrue();
			graveyard.Contains("2").ShouldBeTrue();
			graveyard.Contains("3").ShouldBeTrue();
			graveyard.Contains("4").ShouldBeTrue();
		};

		public class when_adding_items_to_the_newly_created_graveyard
		{
			It should_still_not_track_beyond_the_capacity = () =>
			{
				graveyard.Bury("5");
				graveyard.Bury("6"); // should boot "1" out
				graveyard.Contains("1").ShouldBeFalse();

				graveyard.Contains("2").ShouldBeTrue();
				graveyard.Contains("3").ShouldBeTrue();
				graveyard.Contains("4").ShouldBeTrue();
				graveyard.Contains("5").ShouldBeTrue();
				graveyard.Contains("6").ShouldBeTrue();
			};
		}
		
		static HydratableGraveyard graveyard;
		static GraveyardMemento memento;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
