namespace Hydrospanner
{
	using Disruptor;

	public class DuplicateHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.MessageSequence > 0)
				return; // only filter messages that come off the wire

			data.DuplicateMessage = this.duplicates.Contains(data.WireId);
		}

		public DuplicateHandler(DuplicateStore duplicates)
		{
			this.duplicates = duplicates;
		}

		private readonly DuplicateStore duplicates;
	}
}