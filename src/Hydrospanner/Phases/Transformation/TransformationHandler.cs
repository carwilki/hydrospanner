namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Journal;
	using Snapshot;

	public sealed class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			// Use duplicate handler to dedup (return if forwarded).

			// In the spike code, any messages resulting from here are then pushed back into the TransformationRing by a "forward local handler"
			// instead, let's do the loop here and gather up all of the messages and then push all resulting messages to the next ring buffer as a batch
			// for example, a message arrives and yields two messages; push those two messages to a local buffer
			// for each message in the buffer, push the message in and gather out any resulting messages which are then added to the local buffer
			// and so on until no messages are resulting.
			// then take that entire set of messages (all of which resulted from an incoming message) and push to the next phase as a batch
		}

		public TransformationHandler(
			long journaledSequence, 
			RingBuffer<SnapshotItem> snapshotRing, 
			RingBuffer<JournalItem> journalRing,
			DuplicateHandler duplicates)
		{
		}
	}
}