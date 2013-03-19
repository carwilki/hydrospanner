#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner
{
	using System;
	using Machine.Specifications;

	[Subject(typeof(SystemTime))]
	public class when_generating_utc_now
	{
		It should_reflect_the_actual_clock_time = () =>
		{
			for (var i = 0; i < 100; i++)
				SystemTime.UtcNow.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(1));
		};
	}

	[Subject(typeof(SystemTime))]
	public class when_freezing_system_time
	{
		Establish context = () =>
			SystemTime.Freeze(DateTime.MinValue, DateTime.MaxValue);

		It should_loop_continually_through_the_given_datetime_values = () =>
		{
			SystemTime.UtcNow.ShouldEqual(DateTime.MinValue);
			SystemTime.UtcNow.ShouldEqual(DateTime.MaxValue);

			SystemTime.UtcNow.ShouldEqual(DateTime.MinValue);
			SystemTime.UtcNow.ShouldEqual(DateTime.MaxValue);
		};

		Cleanup after = () =>
			SystemTime.Unfreeze();
	}

	[Subject(typeof(SystemTime))]
	public class when_freezing_with_no_specified_dates
	{
		Because of = () =>
		{
			justBeforeFreeze = SystemTime.UtcNow;
			SystemTime.Freeze();
			afterFreeze = SystemTime.UtcNow;
			afterFreeze2 = SystemTime.UtcNow;
		};

		It should_freeze_at_the_current_time = () =>
			afterFreeze.ShouldBeCloseTo(justBeforeFreeze, TimeSpan.FromMilliseconds(10));

		It should_stay_frozen = () =>
			afterFreeze.ShouldEqual(afterFreeze2);

		Cleanup after = () =>
			SystemTime.Unfreeze();

		static DateTime justBeforeFreeze;
		static DateTime afterFreeze;
		static DateTime afterFreeze2;
	}

	[Subject(typeof(SystemTime))]
	public class when_freezing_with_a_null_collection
	{
		Because of = () =>
		{
			justBeforeFreeze = SystemTime.UtcNow;
			SystemTime.Freeze(null);
			afterFreeze = SystemTime.UtcNow;
			afterFreeze2 = SystemTime.UtcNow;
		};

		It should_freeze_at_the_current_time = () =>
			afterFreeze.ShouldBeCloseTo(justBeforeFreeze, TimeSpan.FromMilliseconds(10));

		It should_stay_frozen = () =>
			afterFreeze.ShouldEqual(afterFreeze2);

		Cleanup after = () =>
			SystemTime.Unfreeze();

		static DateTime justBeforeFreeze;
		static DateTime afterFreeze;
		static DateTime afterFreeze2;
	}

	[Subject(typeof(SystemTime))]
	public class when_time_has_been_frozen_and_then_unfrozen
	{
		Establish context = () =>
			SystemTime.Freeze(DateTime.MinValue);

		Because of = () =>
			SystemTime.Unfreeze();

		It should_reflect_the_actual_clock_time = () =>
			SystemTime.UtcNow.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(1));
	}

	[Subject(typeof(SystemTime))]
	public class when_requesting_epoch_time
	{
		Establish context = () =>
			SystemTime.Freeze(Value);

		It should_be_derived_from_the_current_time = () =>
			SystemTime.EpochUtcNow.ShouldEqual(ExpectedEpochValue);

		Cleanup after = () =>
			SystemTime.Unfreeze();

		const int ExpectedEpochValue = 1360881313;
		static readonly DateTime Value = DateTime.Parse("2/14/2013 10:35:13 PM");
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169