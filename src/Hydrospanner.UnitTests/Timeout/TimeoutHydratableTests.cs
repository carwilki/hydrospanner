#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Wireup;

	[Subject(typeof(TimeoutHydratable))]
	public class when_filtering_messages
	{
		Establish context = () =>
			watcher = new TimeoutHydratable();

		Because of = () =>
			filtered = watcher.Filter(string.Empty, message);

		It should_return_the_object_reference_provided = () =>
			filtered.ShouldEqual(message);

		static readonly object message = new object();
		static ITimeoutWatcher watcher;
		static object filtered;
	}

	[Subject(typeof(TimeoutHydratable))]
	public class when_filtering_DateTime_messages
	{
		Establish context = () =>
			watcher = new TimeoutHydratable();

		Because of = () =>
			filtered = watcher.Filter(SomeKey, instant);

		It should_request_a_timeout_rounded_up_to_the_nearest_second = () =>
			filtered.ShouldBeLike(new TimeoutRequestedEvent(SomeKey, rounded));

		const string SomeKey = "Hello, World!";
		static readonly DateTime rounded = DateTime.Parse("2001-02-03 04:05:07");
		static readonly DateTime instant = DateTime.Parse("2001-02-03 04:05:06").AddTicks(1);
		static readonly object message = new object();
		static ITimeoutWatcher watcher;
		static object filtered;
	}

	public class when_a_hydratable_requests_a_timeout
	{
		Establish context = () =>
			table = new ConventionRoutingTable(new Type[0]);

		Because of = () =>
			routes = table.Lookup(delivery).ToArray();

		It should_receive_the_timeout_without_registering_a_corresponding_lookup = () =>
			routes.Last().Key.ShouldEqual(message.Key);

		It should_throw_an_exception_when_invoking_the_creation_callback = () =>
			Catch.Exception(() => routes.Last().Create()).ShouldBeOfType<InvalidOperationException>();

		static HydrationInfo[] routes; 
		static ConventionRoutingTable table;
		static readonly TimeoutReachedEvent message = new TimeoutReachedEvent("my-key", SystemTime.UtcNow, SystemTime.UtcNow);
		static readonly Delivery<TimeoutReachedEvent> delivery = new Delivery<TimeoutReachedEvent>(
			message, new Dictionary<string, string>(), 1, true, true);
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414