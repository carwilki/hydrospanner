#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using NSubstitute;
	using Phases.Transformation;

	[Subject(typeof(SystemClock))]
	public class when_timing_the_system
	{
		public class when_the_clock_starts
		{
			Establish context = () =>
				timer.Start(Arg.Do<TimerCallback>(x => callback = x));

			Because of = () =>
				clock.Start();
			
			It should_pass_the_callback_to_the_timing_mechanism_for_regular_invocation = () =>
			{
				callback.Invoke(null);
				harness.AllItems.Single().ShouldBeLike(new TransformationItem
				{
					Body = new CurrentTimeMessage(Now),
					IsTransient = true
				});
			};
		}

		public class when_disposing_the_clock
		{
			Establish context = () =>
			{
				clock.Start();
				timer.Start(Arg.Do<TimerCallback>(x => callback = x));
			};

			Because of = () =>
			{
				clock.Dispose();
				Thread.Sleep(50);
			};

			It should_dispose_the_timing_mechanism = () =>
				timer.Received(1).Dispose();

			It should_no_longer_respond_to_requests_to_start_the_timer = () =>
			{
				clock.Start();
				callback.ShouldBeNull();
			};
		}

		public class when_trying_to_start_the_clock_more_than_once
		{
			Establish context = () =>
			{
				callbacks = new List<TimerCallback>();
				timer.Start(Arg.Do<TimerCallback>(x => callbacks.Add(x)));
			};

			Because of = () =>
			{
				for (var i = 0; i < 10; i++)
					clock.Start();
			};

			It should_not_start_the_timing_mechanism_more_than_once = () =>
				callbacks.Count.ShouldEqual(1);

			static List<TimerCallback> callbacks;
		}

		Establish context = () =>
		{
			SystemTime.Freeze(Now);
			timer = Substitute.For<SystemTimer>();
			harness = new RingBufferHarness<TransformationItem>();
			clock = new SystemClock(harness, () => timer);
		};

		Cleanup after = () =>
			SystemTime.Unfreeze();

		static readonly DateTime Now = DateTime.UtcNow;
		static TimerCallback callback;
		static SystemTimer timer;
		static RingBufferHarness<TransformationItem> harness;
		static SystemClock clock;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
