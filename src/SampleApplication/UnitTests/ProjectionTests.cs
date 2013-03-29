#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace SampleApplication.UnitTests
{
	using Machine.Specifications;

	[Subject(typeof(FizzBuzzProjectionHydrator))]
	public class when_handling_events
	{
		public class when_handing_a_count_event
		{
			Because of = () =>
				hydrator.Hydrate(new CountEvent { Value = 42 }, null, false);
			
			It should_set_the_message_to_the_numeric_value = () =>
				hydrator.GetMemento().ShouldBeLike(new FizzBuzzProjection { Value = "42" });
		}

		public class when_handling_a_fizz_event
		{
			Because of = () =>
				hydrator.Hydrate(new FizzEvent { Value = 3 }, null, false);

			It should_set_the_message_to_fizz = () =>
				hydrator.GetMemento().ShouldBeLike(new FizzBuzzProjection { Value = "Fizz" });
		}

		public class when_handling_a_buzz_event
		{
			Because of = () =>
				hydrator.Hydrate(new BuzzEvent { Value = 5 }, null, false);

			It should_set_the_message_to_buzz = () =>
				hydrator.GetMemento().ShouldBeLike(new FizzBuzzProjection { Value = "Buzz" });
		}

		public class when_handling_a_fizzbuzz_event
		{
			Because of = () =>
				hydrator.Hydrate(new FizzBuzzEvent { Value = 15 }, null, false);

			It should_set_the_message_to_fizzbuzz = () =>
				hydrator.GetMemento().ShouldBeLike(new FizzBuzzProjection { Value = "FizzBuzz" });
		}

		Establish context = () =>
			hydrator = new FizzBuzzProjectionHydrator();

		static FizzBuzzProjectionHydrator hydrator;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
