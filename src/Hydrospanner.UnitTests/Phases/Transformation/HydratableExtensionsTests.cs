#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(HydratableExtensions))]
	public class when_routing_messages_to_a_hydratable
	{
		Establish context = () =>
			hydratable = new ApplicationHydratable(string.Empty);

		public class and_the_hydratable_is_null
		{
			It should_not_take_any_action_or_raise_any_error = () =>
				Catch.Exception(() => HydratableExtensions.Hydrate(null, 42, new Dictionary<string, string>(), false)).ShouldBeNull();
		}

		public class and_the_message_is_null
		{
			It should_not_take_any_action_or_raise_any_error = () =>
				Catch.Exception(() => HydratableExtensions.Hydrate(hydratable, null, null, false)).ShouldBeNull();
		}

		public class and_the_message_is_NOT_compatible_with_the_hydratable
		{
			Because of = () =>
				exception = Catch.Exception(() => HydratableExtensions.Hydrate(hydratable, "Not compatible", new Dictionary<string, string>(), true));

			It should_raise_an_error_when_attempting_to_route_the_message = () =>
				exception.ShouldBeOfType<InvalidCastException>();

			static Exception exception;
		}

		public class and_the_message_can_be_handled_by_the_hydratable
		{
			Because of = () =>
				HydratableExtensions.Hydrate(hydratable, 42, new Dictionary<string, string> { { "Hello", "World" } }, true);

			It should_route_according_to_message_type = () =>
			{
				hydratable.PendingMessages.Single().ShouldEqual(42);
				hydratable.metadata.Single().ShouldBeLike(new Dictionary<string, string> { { "Hello", "World" } });
				hydratable.status.Single().ShouldBeTrue();
			};
		}

		static ApplicationHydratable hydratable;
	}

	public class ApplicationHydratable : IHydratable, IHydratable<int>
	{
		public int CurrentState;
		public string Key { get { return this.key; } }
		public bool IsComplete { get { return false; } }
		public bool IsPublicSnapshot { get { return false; } }
		public ICollection<object> PendingMessages { get; private set; }
		public object GetMemento()
		{
			return null;
		}

		public void Hydrate(int message, Dictionary<string, string> headers, bool live)
		{
			this.CurrentState = message;
			this.PendingMessages.Add(message);
			this.metadata.Add(headers);
			this.status.Add(live);
		}

		public ApplicationHydratable(string key)
		{
			this.key = key;
			this.PendingMessages = new List<object>();
		}

		readonly string key;
		public List<Dictionary<string, string>> metadata = new List<Dictionary<string, string>>();
		public List<bool> status = new List<bool>();
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
