namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using Journal;

	public sealed class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			if (this.duplicates.Forward(data))
				return;

			this.Handle(data);
		}

		private void Handle(TransformationItem data)
		{
			var liveMessage = this.Transform(data);
			
			if (liveMessage)
				this.PublishToNextPhase(data);

			this.Increment();
		}

		private bool Transform(TransformationItem data)
		{
			var live = false;

			if (data.MessageSequence == 0)
			{
				data.MessageSequence = this.currentSequnce;
				live = true;
			}

			this.buffer.AddRange(this.transformer.Handle(data));

			for (var i = 0; i < this.buffer.Count; i++)
				this.buffer.AddRange(this.transformer.Handle(this.buffer[i], this.currentSequnce + IncomingMessage + i));

			return live;
		}

		private void PublishToNextPhase(TransformationItem data)
		{
			var batch = this.journalRing.NewBatchDescriptor(this.buffer.Count + IncomingMessage);

			this.journalRing[batch.Start].AsForeignMessage(
				this.currentSequnce, data.SerializedBody, data.Body, data.Headers, data.ForeignId, data.Acknowledgment);

			for (var i = 1; i < this.buffer.Count + IncomingMessage; i++)
				this.journalRing[i + batch.Start]
					.AsTransformationResultMessage(this.currentSequnce + i, this.buffer[i - IncomingMessage], null); // TODO: headers?

			this.journalRing.Publish(batch);
		}

		private void Increment()
		{
			this.snapshot.Increment(this.buffer.Count + IncomingMessage);
			this.currentSequnce += this.buffer.Count + IncomingMessage;
			this.buffer.Clear();
		}

		public TransformationHandler(
			long journaledSequence, 
			IRingBuffer<JournalItem> journalRing,
			IDuplicateHandler duplicates,
			ITransformer transformer,
			ISnapshotTracker snapshot)
		{
			if (journaledSequence < 1)
				throw new ArgumentOutOfRangeException("journaledSequence");

			if (journalRing == null)
				throw new ArgumentNullException("journalRing");

			if (duplicates == null)
				throw new ArgumentNullException("duplicates");

			if (transformer == null)
				throw new ArgumentNullException("transformer");

			if (snapshot == null)
				throw new ArgumentNullException("snapshot");

			this.currentSequnce = journaledSequence + 1;
			this.journalRing = journalRing;
			this.duplicates = duplicates;
			this.transformer = transformer;
			this.snapshot = snapshot;
		}

		private const int IncomingMessage = 1;
		private readonly IRingBuffer<JournalItem> journalRing;
		private readonly IDuplicateHandler duplicates;
		private readonly ITransformer transformer;
		private readonly ISnapshotTracker snapshot;
		private readonly List<object> buffer = new List<object>();
		private long currentSequnce;
	}
}