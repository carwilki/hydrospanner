﻿#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(ConventionRoutingTable))]
	public class when_routing_by_convention
	{
		public class when_providing_a_null_memento
		{
			It should_return_null = () =>
				new ConventionRoutingTable().Create(null).ShouldBeNull();
		}
		public class when_providing_a_memento
		{
			Establish context = () =>
			{
				memento = new TestMemento();
				table = new ConventionRoutingTable(typeof(TestHydratable));
			};

			Because of = () =>
				hydratable = table.Create(memento);

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

			private class MementoHydratable
			{
				public static SomeHydratable Create(TestMemento memento)
				{
					return new SomeHydratable();
				}
			}
			private class DuplicateMementoHydratable
			{
				public static SomeHydratable Create(TestMemento memento)
				{
					return new SomeHydratable();
				}
			}
		}

		public class when_providing_a_null_message
		{
			It should_return_null = () =>
				new ConventionRoutingTable().Lookup(null, null).ShouldBeNull();
		}
		public class when_no_underlying_hydratables_handle_a_given_message
		{
			It should_return_an_empty_set = () =>
				new ConventionRoutingTable().Lookup(new TestMessage(), null).Count().ShouldEqual(0);
		}

		public class when_providing_a_message_and_headers
		{
			Establish context = () =>
			{
				message = new TestMessage();
				table = new ConventionRoutingTable(typeof(TestHydratable), typeof(TestHydratable2));
			};

			Because of = () =>
				list = table.Lookup(message, new Dictionary<string, string>()).ToList();

			It should_return_a_set_of_hydration_info_for_each_registered_hydratable = () =>
			{
				list.Count.ShouldEqual(2);
				list[0].Create().ShouldBeOfType<TestHydratable>();
				list[1].Create().ShouldBeOfType<TestHydratable2>();
			};

			static IRoutingTable table;
			static TestMessage message;
			static List<HydrationInfo> list;
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable ClassNeverInstantiated.Local
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
			public static TestHydratable Create(TestMemento memento)
			{
				return new TestHydratable();
			}
			public static TestHydratable Create()
			{
				throw new NotSupportedException("never executed because of filtering logic within the routing table method selection process.");
			}
			public static TestHydratable Create<T>(TestMemento memento)
			{
				throw new NotSupportedException("never executed because of filtering logic within the routing table method selection process.");
			}

			public static HydrationInfo Lookup(TestMessage message, Dictionary<string, string> headers)
			{
				return new HydrationInfo(string.Empty, () => new TestHydratable());
			}
		}
		private class TestHydratable2 : SomeHydratable
		{
			public static TestHydratable2 Create(TestMemento2 memento)
			{
				return new TestHydratable2();
			}

			public static HydrationInfo Lookup(TestMessage message, Dictionary<string, string> headers)
			{
				return new HydrationInfo(string.Empty, () => new TestHydratable2());
			}
		}
		private class SomeHydratable : IHydratable
		{
			public string Key { get; private set; }
			public bool IsComplete { get; private set; }
			public bool IsPublicSnapshot { get; private set; }
			public IEnumerable<object> GatherMessages()
			{
				throw new NotImplementedException();
			}
			public object GetMemento()
			{
				throw new NotImplementedException();
			}
		}
		// ReSharper restore ClassNeverInstantiated.Local
		// ReSharper restore UnusedMember.Local
	}
}
// ReSharper restore InconsistentNaming
#pragma warning restore 169