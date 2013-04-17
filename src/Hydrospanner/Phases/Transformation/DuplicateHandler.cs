namespace Hydrospanner.Phases.Transformation
{
	using Journal;

	public interface IDuplicateHandler
	{
		bool Forward(TransformationItem item);
	}

	public sealed class DuplicateHandler : IDuplicateHandler
	{
		public bool Forward(TransformationItem item)
		{
			if (item.Acknowledgment == null)
				return false;

			if (!this.store.Contains(item.ForeignId))
				return false;

			this.PublishForAcknowledgement(item);

			return true;
		}
		private void PublishForAcknowledgement(TransformationItem item)
		{
			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsForeignMessage(0, item.SerializedBody, item.Body, item.Headers, item.ForeignId, item.Acknowledgment);
			this.ring.Publish(next);
		}

		public DuplicateHandler(DuplicateStore store, IRingBuffer<JournalItem> ring)
		{
			this.store = store;
			this.ring = ring;
		}

		readonly DuplicateStore store;
		readonly IRingBuffer<JournalItem> ring;
	}

	public sealed class NullDuplicateHandler : IDuplicateHandler
	{
		public bool Forward(TransformationItem item)
		{
			return false;
		}
	}
}