namespace Hydrospanner.Outbox
{
	using Disruptor;

	public class BookmarkHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.IncomingSequence <= this.largest)
				return;

			this.largest = data.IncomingSequence;

			if (!endOfBatch)
				return;

			// update database (reconnect if disconnect) to indicate the highest handled incoming sequence
			// UPDATE bookmarks SET bookmark = data.IncomingSequence;
		}

		private long largest;
	}
}