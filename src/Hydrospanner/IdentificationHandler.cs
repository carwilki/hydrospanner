namespace Hydrospanner
{
	using Disruptor;

	public class IdentificationHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			data.DuplicateMessage = this.duplicates.Contains(data.WireId);
			if (data.DuplicateMessage)
				return; // all event handlers hereafter should ignore this message

			data.StreamId = this.identifier.DiscoverStreams(data.Body, data.Headers);
		}

		public IdentificationHandler(IStreamIdentifier identifier, DuplicateStore duplicates)
		{
			this.identifier = identifier;
			this.duplicates = duplicates;
		}

		private readonly IStreamIdentifier identifier;
		private readonly DuplicateStore duplicates;
	}
}