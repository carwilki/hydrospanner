namespace Hydrospanner
{
	using System.Collections.Generic;
	using Disruptor;

	public class TransformationHandler : IEventHandler<TransformationMessage>
	{
		public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
			this.Hydrate(data);
			this.PublishMessage(data.MessageSequence);
		}

		private void Hydrate(TransformationMessage data)
		{
			foreach (var hydratable in data.Hydratables)
			{
				hydratable.Hydrate(data.Body, data.Headers, data.Replay);

				// TODO: need to understand the ramifications of rebuilding a given projection...(or aggregate or saga???)
				if (!data.Replay)
					this.messages.AddRange(hydratable.GatherMessages());
			}
		}
		private void PublishMessage(long sourceSequence)
		{
			if (this.messages.Count == 0)
				return;

			var descriptor = this.ring.NewBatchDescriptor(this.messages.Count);
			descriptor = this.ring.Next(descriptor);

			for (var i = 0; i < this.messages.Count; i++)
			{
				var claimed = this.ring[descriptor.Start + i];
				claimed.Clear();

				claimed.Body = this.messages[i];
				claimed.Headers = new Dictionary<string, string>(); // TODO
				claimed.SourceSequence = sourceSequence;
			}

			this.ring.Publish(descriptor);
			this.messages.Clear();
		}

		public TransformationHandler(RingBuffer<WireMessage> ring)
		{
			this.ring = ring;
		}

		private readonly List<object> messages = new List<object>();
		private readonly RingBuffer<WireMessage> ring;
	}
}