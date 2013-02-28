namespace Hydrospanner.Phase2
{
	using Disruptor;

	public class TransformationHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			for (var i = 0; i < data.Hydratables.Count; i++)
			{
				var hydratable = data.Hydratables[i];
				hydratable.Hydrate(data.Body, data.Headers);
			}

			// TODO: now gather up the resulting "state"
		}
	}
}