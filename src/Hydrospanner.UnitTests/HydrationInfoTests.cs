#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner
{
	using Machine.Specifications;

	[Subject(typeof(HydrationInfo))]
	public class with_hydration_info
	{
		public class when_constructing
		{
			It should_throw_when_the_key_is_null = () =>
				Catch.Exception(() => new HydrationInfo(null, () => null));

			It should_throw_when_the_factory_is_null = () =>
				Catch.Exception(() => new HydrationInfo(string.Empty, null));
		}

		public class when_resolving_an_uninitialized_info
		{
			It should_return_a_null_key = () =>
				new HydrationInfo().Key.ShouldBeNull();

			It should_return_a_null_hydratable = () =>
				new HydrationInfo().Create().ShouldBeNull();
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
