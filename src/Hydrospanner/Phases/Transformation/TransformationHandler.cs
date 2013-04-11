﻿namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using Journal;
	using log4net;

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

			this.Increment(data);
		}

		private bool Transform(TransformationItem data)
		{
			var live = false;

			if (data.MessageSequence == 0)
			{
				data.MessageSequence = this.currentSequnce + 1;
				live = true;
			}

			this.buffer.Clear(); // TODO: get this under test (for when we bail out during replay scenarios according to TODO condition below)

			Log.DebugFormat("Transforming hydratables that subscribe to type {0}.", data.SerializedType);
			this.buffer.AddRange(this.transformer.Handle(data.Body, data.Headers, data.MessageSequence));

			// TODO: bug: if we're replaying and haven't caught the live stream, DON'T gather any messages from the aggregate
			// instead, we should return live (which will be false)
			// during replay, each message will be a TransformationItem and replay SHOULD NOT produce any additional messages

			for (var i = 0; i < this.buffer.Count; i++)
			{
				Log.Debug("Now transforming additional hydratables that subscribe to messages generated by original and subsequent transformation.");
				this.buffer.AddRange(this.transformer.Handle(this.buffer[i], BlankHeaders, data.MessageSequence + 1 + i));
			}

			return live;
		}

		private void PublishToJournalPhase(TransformationItem data)
		{
			Log.DebugFormat("Publishing {0} items to the Journal Disruptor.", this.buffer.Count + IncomingMessage);

			var size = this.buffer.Count + IncomingMessage;
			var batch = this.journalRing.Next(size);

			this.journalRing[batch.Start].AsForeignMessage(
				this.currentSequnce + 1, data.SerializedBody, data.Body, data.Headers, data.ForeignId, data.Acknowledgment);

			for (var i = 1; i < size; i++)
				this.journalRing[i + batch.Start].AsTransformationResultMessage(this.currentSequnce + 1 + i, this.buffer[i - IncomingMessage], BlankHeaders);

			this.journalRing.Publish(batch);
		}

		private void Increment(TransformationItem data)
		{
			if (data.MessageSequence > this.currentSequnce)
			{
				this.currentSequnce += this.buffer.Count + IncomingMessage;
				this.snapshot.Track(this.currentSequnce);
			}

			this.buffer.Clear();
		}

		public TransformationHandler(
			long journaledSequence, 
			IRingBuffer<JournalItem> journalRing,
			IDuplicateHandler duplicates,
			ITransformer transformer,
			ISystemSnapshotTracker snapshot)
		{
			if (journaledSequence < 0)
				throw new ArgumentOutOfRangeException("journaledSequence");

			if (journalRing == null)
				throw new ArgumentNullException("journalRing");

			if (duplicates == null)
				throw new ArgumentNullException("duplicates");

			if (transformer == null)
				throw new ArgumentNullException("transformer");

			if (snapshot == null)
				throw new ArgumentNullException("snapshot");

			this.currentSequnce = journaledSequence;
			this.journalRing = journalRing;
			this.duplicates = duplicates;
			this.transformer = transformer;
			this.snapshot = snapshot;
		}

		private const int IncomingMessage = 1;
		private static readonly ILog Log = LogManager.GetLogger(typeof(TransformationHandler));
		private static readonly Dictionary<string, string> BlankHeaders = new Dictionary<string, string>(); 
		private readonly IRingBuffer<JournalItem> journalRing;
		private readonly IDuplicateHandler duplicates;
		private readonly ITransformer transformer;
		private readonly ISystemSnapshotTracker snapshot;
		private readonly List<object> buffer = new List<object>();
		private long currentSequnce;
	}
}