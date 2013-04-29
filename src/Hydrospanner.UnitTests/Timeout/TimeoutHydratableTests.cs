#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Timeout
{
	using System;
	using Machine.Specifications;

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

		It should_return_the_object_reference_provided = () =>
			filtered.ShouldBeLike(new TimeoutRequestedEvent(SomeKey, instant));

		const string SomeKey = "Hello, World!";
		static readonly DateTime instant = DateTime.UtcNow;
		static readonly object message = new object();
		static ITimeoutWatcher watcher;
		static object filtered;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414