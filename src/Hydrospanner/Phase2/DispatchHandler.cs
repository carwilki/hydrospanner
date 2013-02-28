namespace Hydrospanner.Phase2
{
	using System.Collections.Generic;
	using Disruptor;

	public class DispatchHandler : IEventHandler<ParsedMessage>
	{
		public void OnNext(ParsedMessage data, long sequence, bool endOfBatch)
		{
			if (!data.IncomingWireMessage)
				return;

			pending.AddRange(data.PendingDispatch);

			if (endOfBatch)
			{
				// TODO: dispatch here
			}
		}

		private readonly List<object> pending = new List<object>();
	}
}