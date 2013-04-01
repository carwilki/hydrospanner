#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace SampleApplication.UnitTests
{
	using Machine.Specifications;

	[Subject(typeof(FizzBuzzAggregate))]
	public class when_receiving_the_live_stream
	{
		Establish context = () =>
			aggregate = new FizzBuzzAggregateHydrator();

		Because of = () =>
		{
			aggregate.Hydrate(new CountCommand { Value = 1 }, null, true);
			aggregate.Hydrate(new CountCommand { Value = 3 }, null, true);
			aggregate.Hydrate(new CountCommand { Value = 5 }, null, true);
			aggregate.Hydrate(new CountCommand { Value = 15 }, null, true);
			aggregate.Hydrate(new CountCommand { Value = 17 }, null, true);
		};

		It should_gather_the_resulting_messages_after_transformation = () => aggregate.GatherMessages().ShouldBeLike(new object[]
		{
			new CountEvent { Value = 1 },
			new FizzEvent { Value = 3 },
			new BuzzEvent { Value = 5 },
			new FizzBuzzEvent { Value = 15 },
			new CountEvent { Value = 17 }
		});

		static FizzBuzzAggregateHydrator aggregate;
	}

	public class when_applying_replay_events
	{
		Establish context = () =>
			aggregate = new FizzBuzzAggregateHydrator();

		Because of = () =>
		{
			aggregate.Hydrate(new CountEvent { Value = 1 }, null, false);
			aggregate.Hydrate(new FizzEvent { Value = 3 }, null, false);
			aggregate.Hydrate(new BuzzEvent { Value = 5 }, null, false);
			aggregate.Hydrate(new FizzBuzzEvent { Value = 15 }, null, false);
			aggregate.Hydrate(new CountEvent { Value = 17 }, null, false);
		};

		It should_provide_the_correct_snapshot = () =>
			aggregate.GetMemento().ShouldEqual(17);

		static FizzBuzzAggregateHydrator aggregate;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
