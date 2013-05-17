#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Timeout;

	[Subject(typeof(ConventionRoutingTable))]
	public class when_routing_by_convention
	{
		public class when_providing_a_null_memento
		{
			It should_return_null = () =>
				new ConventionRoutingTable().Restore<string>("key", null).ShouldBeNull();
		}
		public class when_providing_a_memento
		{
			Establish context = () =>
			{
				memento = new TestMemento();
				table = new ConventionRoutingTable(typeof(TestHydratable));
			};

			Because of = () =>
				hydratable = table.Restore("key", memento);

			It should_create_a_hydratable = () =>
				hydratable.ShouldBeOfType<TestHydratable>();

			static IRoutingTable table;
			static TestMemento memento;
			static IHydratable hydratable;
		}
		public class when_a_memento_type_is_already_registered_to_another_hydratable
		{
			Because of = () =>
				thrown = Catch.Exception(() => new ConventionRoutingTable(typeof(MementoHydratable), typeof(DuplicateMementoHydratable)));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<InvalidOperationException>();

			static Exception thrown;

			// ReSharper disable UnusedMember.Local
			// ReSharper disable UnusedParameter.Local
			private class MementoHydratable
			{
				public static SomeHydratable Restore(string key, TestMemento memento)
				{
					return new SomeHydratable();
				}
			}
			private class DuplicateMementoHydratable
			{
				public static SomeHydratable Restore(string key, TestMemento memento)
				{
					return new SomeHydratable();
				}
			}
			// ReSharper restore UnusedParameter.Local
			// ReSharper restore UnusedMember.Local
		}

		public class when_providing_a_null_message
		{
			It should_return_null = () =>
				new ConventionRoutingTable().Lookup(new Delivery<object>()).ShouldBeEmpty();
		}
		public class when_no_underlying_hydratables_handle_a_given_message
		{
			It should_return_an_empty_set = () =>
				new ConventionRoutingTable().Lookup(new Delivery<TestMessage>(new TestMessage(), null, 1, true, true)).Count().ShouldEqual(0);
		}

		public class when_providing_a_delivery
		{
			Establish context = () =>
			{
				message = new TestMessage();
				table = new ConventionRoutingTable(typeof(TestHydratable), typeof(TestHydratable2));
			};

			Because of = () =>
				list = table.Lookup(new Delivery<TestMessage>(message, new Dictionary<string, string>(), 1, true, true)).ToList();

			It should_return_a_set_of_hydration_info_structs_for_each_registered_hydratable = () =>
			{
				list.Count.ShouldEqual(2);
				list[0].Create().ShouldBeOfType<TestHydratable>();
				list[1].Create().ShouldBeOfType<TestHydratable2>();
			};

			static IRoutingTable table;
			static TestMessage message;
			static List<HydrationInfo> list;
		}

		public class when_registering_a_set_of_types
		{
			Establish context = () =>
				table = new ConventionRoutingTable(typeof(TestHydratable), typeof(TestHydratable2));

			It should_also_register_all_internal_types = () =>
				table.Restore("key", new TimeoutMemento()).ShouldBeOfType<TimeoutHydratable>();

			static IRoutingTable table;
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable UnusedTypeParameter
		// ReSharper disable UnusedParameter.Local
		// ReSharper disable ClassNeverInstantiated.Local
		// ReSharper disable UnusedAutoPropertyAccessor.Local
		private class TestMemento
		{
		}
		private class TestMemento2
		{
		}
		public class TestMessage
		{
		}
		private class TestHydratable : SomeHydratable
		{
			public static TestHydratable Restore(string key, TestMemento memento)
			{
				return new TestHydratable();
			}
			public static TestHydratable Restore()
			{
				throw new NotSupportedException("never executed because of filtering logic within the routing table method selection process.");
			}
			public static TestHydratable Restore<T>(string key, TestMemento memento)
			{
				throw new NotSupportedException("never executed because of filtering logic within the routing table method selection process.");
			}

			public static HydrationInfo Lookup(Delivery<TestMessage> delivery)
			{
				return new HydrationInfo(string.Empty, () => new TestHydratable());
			}
		}
		private class TestHydratable2 : SomeHydratable
		{
			public static TestHydratable2 Restore(string key, TestMemento2 memento)
			{
				return new TestHydratable2();
			}

			public static HydrationInfo Lookup(Delivery<TestMessage> delivery)
			{
				return new HydrationInfo(string.Empty, () => new TestHydratable2());
			}
		}
		private class SomeHydratable : IHydratable
		{
			public string Key { get; private set; }
			public bool IsComplete { get; private set; }
			public bool IsPublicSnapshot { get; private set; }
			public object Memento { get; private set; }
			public Type MementoType { get; private set; }
			public ICollection<object> PendingMessages { get; private set; }
		}
		// ReSharper restore UnusedAutoPropertyAccessor.Local
		// ReSharper restore ClassNeverInstantiated.Local
		// ReSharper restore UnusedParameter.Local
		// ReSharper restore UnusedTypeParameter
		// ReSharper restore UnusedMember.Local
	}
}
// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414