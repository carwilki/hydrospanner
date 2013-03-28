﻿namespace Hydrospanner.Phases.Transformation
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
				this.PublishToJournalPhase(data);

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

			this.buffer.AddRange(this.transformer.Handle(data.Body, data.Headers, live));

			for (var i = 0; i < this.buffer.Count; i++)
				this.buffer.AddRange(this.transformer.Handle(this.buffer[i], BlankHeaders, live));

			return live;
		}

		private void PublishToJournalPhase(TransformationItem data)
		{
			var size = this.buffer.Count + IncomingMessage;
			var batch = this.journalRing.NewBatchDescriptor(size);

			this.journalRing[batch.Start].AsForeignMessage(
				this.currentSequnce, data.SerializedBody, data.Body, data.Headers, data.ForeignId, data.Acknowledgment);

			for (var i = 1; i < size; i++)
				this.journalRing[i + batch.Start].AsTransformationResultMessage(this.currentSequnce + i, this.buffer[i - IncomingMessage], BlankHeaders);

			this.journalRing.Publish(batch);
		}

		private void Increment()
		{
			var count = this.buffer.Count + IncomingMessage;
			this.snapshot.Increment(count);
			this.currentSequnce += count;
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
		private static readonly Dictionary<string, string> BlankHeaders = new Dictionary<string, string>(); 
		private readonly IRingBuffer<JournalItem> journalRing;
		private readonly IDuplicateHandler duplicates;
		private readonly ITransformer transformer;
		private readonly ISnapshotTracker snapshot;
		private readonly List<object> buffer = new List<object>();
		private long currentSequnce;
	}
}