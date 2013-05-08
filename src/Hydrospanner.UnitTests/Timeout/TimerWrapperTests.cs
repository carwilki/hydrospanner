#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Timeout
{
	using System;
	using System.Threading;
	using Machine.Specifications;

	[Subject(typeof(TimerWrapper))]
	public class when_using_the_timer_wrapper
	{
		public class when_constructing_a_timer
		{
			Establish context = () =>
				invocations = 0;

			Because of = () =>
			{
				timer.Start(x => invocations++);
				Thread.Sleep(10);
			};

			It should_invoke_the_callback_immediately_when_started = () =>
				invocations.ShouldEqual(1);

			static int invocations;
		}

		public class when_running_the_timer
		{
			Establish context = () =>
			{
				interval = TimeSpan.FromMilliseconds(1);
				timer = new TimerWrapper(interval);
				invocations = 0;
			};

			Because of = () =>
			{
				timer.Start(x => invocations++);
				Thread.Sleep(25);
			};

			It should_invoke_the_callback_for_each_interval = () =>
				invocations.ShouldBeGreaterThan(1);

			static int invocations;
		}

		public class when_disposing_the_timer
		{
			Establish context = () =>
			{
				interval = TimeSpan.FromMilliseconds(1);
				timer = new TimerWrapper(interval);
				invocations = 0;
				timer.Start(x => invocations++);
			};

			Because of = () =>
			{
				timer.Dispose();
				Thread.Sleep(1);
				invocationsAtDisposal = invocations;
				Thread.Sleep(1);
			};

			It should_no_longer_invoke_the_callback = () =>
				invocations.ShouldEqual(invocationsAtDisposal);

			static int invocationsAtDisposal;
			static int invocations;
		}

		Establish context = () =>
		{
			interval = TimeSpan.FromSeconds(1);
			timer = new TimerWrapper(interval);
		};

		Cleanup after = () =>
			timer.Dispose();

		static TimerWrapper timer;
		static TimeSpan interval;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414