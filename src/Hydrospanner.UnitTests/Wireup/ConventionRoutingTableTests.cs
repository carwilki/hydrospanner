#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Wireup
{
	using System.Collections.Generic;
	using Machine.Specifications;

	[Subject(typeof(ConventionRoutingTable))]
	public class when_routing_by_convention
	{
		public class when_providing_a_null_memento
		{
			It should_return_null = () =>
				new ConventionRoutingTable().Create(null).ShouldBeNull();
		}
		[Ignore("Still testing...")]
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

		private class TestMemento
		{
		}
		private class TestHydratable : IHydratable
		{
			public static TestHydratable Create(TestMemento memento)
			{
				return new TestHydratable();
			}

			public string Key { get; private set; }
			public bool IsComplete { get; private set; }
			public bool IsPublicSnapshot { get; private set; }
			public IEnumerable<object> GatherMessages()
			{
				throw new System.NotImplementedException();
			}
			public object GetMemento()
			{
				throw new System.NotImplementedException();
			}
		}
	}
}
// ReSharper restore InconsistentNaming
#pragma warning restore 169