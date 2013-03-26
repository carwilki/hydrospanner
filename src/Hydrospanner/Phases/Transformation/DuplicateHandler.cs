﻿namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Journal;

	public class DuplicateHandler
	{
		public virtual bool Forward(TransformationItem item)
		{
			if (item.Acknowledgment == null)
				return false;

			item.IsDuplicate = this.store.Contains(item.ForeignId);

			if (!item.IsDuplicate)
				return false;

			this.PublishForAcknowledgement(item);

			return true;
		}

		void PublishForAcknowledgement(TransformationItem item)
		{
			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsForeignMessage(
				item.MessageSequence, item.SerializedBody, item.Body, item.Headers, item.ForeignId, item.Acknowledgment);
			this.ring.Publish(next);
		}

		public DuplicateHandler(DuplicateStore store, RingBuffer<JournalItem> ring)
		{
			this.store = store;
			this.ring = ring;
		}

		readonly DuplicateStore store;
		readonly RingBuffer<JournalItem> ring;
	}
}