namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;
	using Hydrospanner.Wireup;

	public class FizzBuzzRoutingTable : IRoutingTable
	{
		// This is a temporary resources, which will be made obsolete when the ConventionRoutingTable is finished

		public IEnumerable<HydrationInfo> Lookup(object message, Dictionary<string, string> headers)
		{
			if (message is CountCommand)
				yield return new HydrationInfo(FizzBuzzAggregateHydrator.TheKey, () => new FizzBuzzAggregateHydrator());
			else if (message is CountEvent || message is FizzEvent || message is BuzzEvent || message is FizzBuzzEvent)
			{
				yield return new HydrationInfo(FizzBuzzAggregateHydrator.TheKey, () => new FizzBuzzAggregateHydrator());
				yield return new HydrationInfo(FizzBuzzProjectionHydrator.TheKey, () => new FizzBuzzProjectionHydrator());
			}
			else
				throw new NotSupportedException(
					string.Format("Unable to find keys for IHydrables based on message of type {0}", message.GetType()));
		}

		public IHydratable Create(object memento)
		{
			if (memento is int)
				return new FizzBuzzAggregateHydrator((int)memento);

			if (memento is FizzBuzzProjection)
				return new FizzBuzzProjectionHydrator(memento as FizzBuzzProjection);

			throw new NotSupportedException(
				string.Format("Unable to create IHydratable from memento of type {0}", memento.GetType()));
		}
	}
}