#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
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
				hydratable.messages.Single().ShouldEqual(42);
				hydratable.metadata.Single().ShouldBeLike(new Dictionary<string, string> { { "Hello", "World" } });
				hydratable.status.Single().ShouldBeTrue();
			};
		}

		static ApplicationHydratable hydratable;
	}

	[Subject(typeof(HydratableExtensions))]
	public class when_selecting_keys_for_a_given_message
	{
		Establish context = () =>
			applicationSelector = new ApplicationSelector();

		public class and_the_selector_is_null
		{
			Because of = () =>
			{
				exception = Catch.Exception(() =>
				{
					result = HydratableExtensions.Keys(null, new object(), new Dictionary<string, string>());
				});
			};

			It should_not_take_any_action_or_raise_any_error = () =>
			{
				exception.ShouldBeNull();
				result.ShouldBeNull();
			};

			static Exception exception;
			static IEnumerable<IHydratableKey> result;
		}

		public class and_the_message_is_null
		{
			Because of = () =>
			{
				exception = Catch.Exception(() =>
				{
					result = HydratableExtensions.Keys(applicationSelector, null, new Dictionary<string, string>());
				});
			};

			It should_not_take_any_action_or_raise_any_error = () =>
			{
				exception.ShouldBeNull();
				result.ShouldBeNull();
			};

			static Exception exception;
			static IEnumerable<IHydratableKey> result;
		}

		public class and_the_selector_is_compatible_with_the_message
		{
			Because of = () =>
				keys = HydratableExtensions.Keys(applicationSelector, 42);

			It should_return_the_keys = () =>
				keys.Single().Name.ShouldEqual("42");
		}

		public class and_the_selector_is_NOT_compatible_with_the_message
		{
			Because of = () =>
			{
				exception = Catch.Exception(() => 
					HydratableExtensions.Keys(applicationSelector, "blah", new Dictionary<string, string>()));
			};

			It should_raise_an_error = () =>
				exception.ShouldBeOfType<InvalidCastException>();

			static Exception exception;
		}

		static IEnumerable<IHydratableKey> keys; 
		static ApplicationSelector applicationSelector;
	}

	[Subject(typeof(HydratableExtensions))]
	public class when_selecting_a_hydratable_from_a_memento
	{
		Establish context = () =>
			applicationSelector = new ApplicationSelector();

		public class when_the_selector_is_null
		{
			Because of = () =>
			{
				exception = Catch.Exception(() =>
				{
					result = HydratableExtensions.Create(null, new object());
				});
			};

			It should_not_take_any_action_or_raise_any_error = () =>
			{
				exception.ShouldBeNull();
				result.ShouldBeNull();
			};

			static Exception exception;
			static IHydratable result;
		}

		public class when_the_memento_can_be_used_to_construct_a_hydratable
		{
			Because of = () =>
				hydratable = HydratableExtensions.Create(applicationSelector, 42);

			It should_construct_the_hydratable = () =>
				((ApplicationHydratable)hydratable).Key.ShouldEqual("42");
		}

		public class when_the_memento_CANNOT_be_used_to_construct_a_hydratable
		{
			Because of = () =>
				exception = Catch.Exception(() => HydratableExtensions.Create(applicationSelector, "blah"));

			It should_raise_an_error = () =>
				exception.ShouldBeOfType<InvalidCastException>();

			static Exception exception;
		}

		static IHydratable hydratable;
		static ApplicationSelector applicationSelector;
	}

	public class ApplicationSelector : IHydratableSelector<int>, IHydratableFactory<int>
	{
		public IEnumerable<IHydratableKey> Keys(int message, Dictionary<string, string> headers = null)
		{
			yield return new ApplicationKey(message.ToString(CultureInfo.InvariantCulture));
		}

		public IHydratable Create(int memento)
		{
			return new ApplicationHydratable(memento.ToString(CultureInfo.InvariantCulture));
		}
	}

	public class ApplicationKey : IHydratableKey
	{
		public string Name { get { return this.key; } }
		public IHydratable Create()
		{
			return new ApplicationHydratable(this.key);
		}

		public ApplicationKey(string key)
		{
			this.key = key;
		}

		readonly string key;
	}

	public class ApplicationHydratable : IHydratable, IHydratable<int>
	{
		public int CurrentState;
		public string Key { get { return this.key; } }
		public bool IsComplete { get { return false; } }
		public bool IsPublicSnapshot { get { return false; } }
		public IEnumerable<object> GatherMessages()
		{
			yield break;
		}

		public object GetMemento()
		{
			return null;
		}

		public void Hydrate(int message, Dictionary<string, string> headers, bool live)
		{
			this.CurrentState = message;
			this.messages.Add(message);
			this.metadata.Add(headers);
			this.status.Add(live);
		}

		public ApplicationHydratable(string key)
		{
			this.key = key;
		}

		readonly string key;
		public List<int> messages = new List<int>();
		public List<Dictionary<string, string>> metadata = new List<Dictionary<string, string>>();
		public List<bool> status = new List<bool>();
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
