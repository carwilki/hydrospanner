namespace SampleApplication
{
	public class HydratableKeys
	{
		public const string AggregateKey = "/aggregates/fizzbuzz/{0}"; // {0}: StreamId
		public const string ProjectionKey = "/projections/fizzbuzz/{0}"; // {0}: StreamId
	}
}