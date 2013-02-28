namespace Hydrospanner.Phase2
{
	using System.Collections.Generic;
	using Disruptor;

	public class TransformationHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (data.IncomingWireMessage)
				data.PendingDispatch = new List<object>();

			for (var i = 0; i < data.Hydratables.Count; i++)
			{
				var hydratable = data.Hydratables[i];
				hydratable.Hydrate(data.Body, data.Headers);

				if (data.IncomingWireMessage)
					data.PendingDispatch.AddRange(hydratable.GatherMessages());
			}
		}
	}
}