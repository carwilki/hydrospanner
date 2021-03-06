﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(DuplicateStore))]
	public class when_prepopulating_the_store
	{
		Because of = () =>
			store = new DuplicateStore(1024, contains);

		It should_contain_the_keys_provided = () =>
			contains.ToList().ForEach(x => store.Contains(x).ShouldBeTrue());

		static readonly Guid[] contains = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
		static DuplicateStore store;
	}

	[Subject(typeof(DuplicateStore))]
	public class when_tracking_duplicates
	{
		It should_never_track_empty_keys = () =>
		{
			store.Contains(Guid.Empty).ShouldBeFalse();
			store.Contains(Guid.Empty).ShouldBeFalse();
			store.Contains(Guid.Empty).ShouldBeFalse();
		};

		public class when_tracking_at_or_below_capacity
		{
			Because of = () =>
			{
				store.Contains(A).ShouldBeFalse();
				store.Contains(B).ShouldBeFalse();
				store.Contains(C).ShouldBeFalse();
				store.Contains(D).ShouldBeFalse();
			};

			It should_track_all_keys = () =>
			{
				store.Contains(A).ShouldBeTrue();
				store.Contains(B).ShouldBeTrue();
				store.Contains(C).ShouldBeTrue();
				store.Contains(D).ShouldBeTrue();
			};
		}

		public class when_tracking_above_capacity
		{
			Establish context = () =>
			{
				store.Contains(A);
				store.Contains(B);
				store.Contains(C);
				store.Contains(D);
			};

			Because of = () =>
				store.Contains(Guid.NewGuid()); // causes A to be forgotten

			It should_only_remember_the_newer_keys_within_capacity = () =>
			{
				store.Contains(B).ShouldBeTrue();
				store.Contains(C).ShouldBeTrue();
				store.Contains(D).ShouldBeTrue();
				store.Contains(A).ShouldBeFalse();
			};
		}

		Establish context = () =>
			store = new DuplicateStore(4);

		static readonly Guid A = new Guid("11111111-0000-0000-0000-000000000000");
		static readonly Guid B = new Guid("22222222-0000-0000-0000-000000000000");
		static readonly Guid C = new Guid("33333333-0000-0000-0000-000000000000");
		static readonly Guid D = new Guid("44444444-0000-0000-0000-000000000000");
		static DuplicateStore store;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
