namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Journal;
	using Snapshot;

	public sealed class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			// TODO: perform de-duplication here, e.g. if the item is duplicate, just forward the item to the next ring as an "ack-only" message and then return
			// also, de-duplication must only happen on *live* incoming "foreign" messages that have just arrived off the wire, e.g.:
			// if (data.Acknowledgment != null) data.IsDuplicate = this.duplicates.Contains(data.ForeignId);

			// TODO: in the spike code, any messages resulting from here are then pushed back into the TransformationRing by a "forward local handler"
			// instead, let's do the loop here and gather up all of the messages and then push all resulting messages to the next ring buffer as a batch
			// for example, a message arrives and yields two messages; push those two messages to a local buffer
			// for each message in the buffer, push the message in and gather out any resulting messages which are then added to the local buffer
			// and so on until no messages are resulting.
			// then take that entire set of messages (all of which resulted from an incoming message) and push to the next phase as a batch
		}

		public TransformationHandler(long journaledSequence, RingBuffer<SnapshotItem> snapshotRing, RingBuffer<JournalItem> journalRing)
		{
		}
	}
}