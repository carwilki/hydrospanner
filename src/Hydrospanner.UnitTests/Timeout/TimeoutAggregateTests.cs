#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(TimeoutAggregate))]
	public class when_handling_timeouts
	{
		public class when_a_pending_timeout_has_been_reached
		{
			Establish context = () =>
				init.Add(new TimeoutRequestedEvent(Key, instant));

			It should_raise_a_timeout_reached_event = () =>
				pending.Single().ShouldBeLike(new TimeoutReachedEvent(Key, instant, SystemTime.UtcNow));

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(-30);
		}

		public class when_a_pending_timeout_has_NOT_been_reached
		{
			Establish context = () =>
				init.Add(new TimeoutRequestedEvent(Key, instant));

			It should_NOT_raise_a_timeout_reached_event = () =>
				pending.ShouldBeEmpty();

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(30);
		}

		public class when_a_pending_timeout_is_aborted
		{
			Establish context = () =>
			{
				init.Add(new TimeoutRequestedEvent(Key, instant));
				init.Add(new TimeoutRequestedEvent(Key, instant + TimeSpan.FromSeconds(1)));
				becauseOf = () => aggregate.AbortTimeouts(Key);
			};

			It should_raise_timeout_aborted_events = () =>
				pending.ShouldBeLike(new[]
				{
					new TimeoutAbortedEvent(Key, instant),
					new TimeoutAbortedEvent(Key, instant + TimeSpan.FromSeconds(1))
				});

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(30);
		}

		public class when_restoring_from_a_snapshot
		{
			Establish context = () =>
			{
				init.Add(new TimeoutRequestedEvent(Key, instant));
				init.Add(new TimeoutRequestedEvent(Key, instant + TimeSpan.FromSeconds(1)));
				becauseOf = () =>
				{
					var memento = aggregate.Clone();
					aggregate = new TimeoutAggregate(pending);
					aggregate.Restore(memento as TimeoutMemento);
					aggregate.DispatchTimeouts(SystemTime.UtcNow);
				};
			};

			It should_correctly_restore_the_internal_state = () =>
				pending.ShouldBeLike(new[]
				{
					new TimeoutReachedEvent(Key, instant, SystemTime.UtcNow),
					new TimeoutReachedEvent(Key, instant + TimeSpan.FromSeconds(1), SystemTime.UtcNow)
				});

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(-30);
		}

		public class when_an_aborted_timeout_has_been_reached
		{
			Establish context = () =>
			{
				init.Add(new TimeoutRequestedEvent(Key, instant));
				init.Add(new TimeoutAbortedEvent(Key, instant));
				becauseOf = () => aggregate.DispatchTimeouts(instant.AddSeconds(1));
			};

			It should_NOT_raise_any_events = () =>
				pending.ShouldBeEmpty();

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(30);
		}

		public class when_a_timeout_has_been_reached_dispatched_and_time_continues
		{
			Establish context = () =>
			{
				init.Add(new TimeoutRequestedEvent(Key, instant));
				init.Add(new TimeoutReachedEvent(Key, instant, SystemTime.UtcNow));
				becauseOf = () => aggregate.DispatchTimeouts(instant.AddSeconds(1));
			};

			It should_NOT_raise_any_events_for_the_reached_timeout = () =>
				pending.ShouldBeEmpty();

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(30);
		}

		public class when_a_timeout_is_reached_for_a_timeout_that_was_not_found
		{
			Establish context = () =>
			{
				init.Add(new TimeoutReachedEvent("key not found", instant, SystemTime.UtcNow));
				becauseOf = () => aggregate.DispatchTimeouts(instant.AddSeconds(1));
			};

			It should_NOT_raise_any_events_for_the_reached_timeout = () =>
				pending.ShouldBeEmpty();

			static readonly DateTime instant = SystemTime.UtcNow.AddMinutes(30);
		}

		Establish context = () =>
		{
			SystemTime.Freeze(DateTime.UtcNow);
			pending = new List<object>();
			aggregate = new TimeoutAggregate(pending);
			init = new List<object>();
			becauseOf = () => aggregate.DispatchTimeouts(SystemTime.UtcNow);
		};

		Because of = () =>
		{
			foreach (var message in init)
			{
				if (message is TimeoutRequestedEvent)
					aggregate.Apply(message as TimeoutRequestedEvent);
				else if (message is TimeoutAbortedEvent)
					aggregate.Apply(message as TimeoutAbortedEvent);
				else if (message is TimeoutReachedEvent)
					aggregate.Apply(message as TimeoutReachedEvent);
				else
					throw new NotSupportedException();
			}

			if (becauseOf != null)
				becauseOf();
		};

		Cleanup after = () =>
			SystemTime.Unfreeze();

		static readonly string Key = Guid.NewGuid().ToString();
		static List<object> init; 
		static List<object> pending;
		static TimeoutAggregate aggregate;
		static Action becauseOf;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414