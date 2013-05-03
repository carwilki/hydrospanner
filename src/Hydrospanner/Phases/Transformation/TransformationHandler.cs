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
			this.item = data;

			if (this.Skip())
				return;

			var liveMessage = this.Transform();

			if (liveMessage)
				this.PublishToJournalPhase();

			this.Increment();
		}
		private bool Skip()
		{
			if (this.skipAllRemaining)
				return true;

			var body = this.item.Body != null; // null body == serialization failure
			if (this.item.MessageSequence == 0 && !body)
				return true;

			return this.skipAllRemaining = this.item.MessageSequence > 0 && !body;
		}
		private bool Transform()
		{
			var live = false;
			if (this.item.MessageSequence == 0)
			{
				this.offset = this.item.IsTransient ? 0 : IncludeIncomingMessage;
				this.item.MessageSequence = this.currentSequnce + this.offset;
				live = true;
			}

			Log.DebugFormat("Transforming hydratables that subscribe to type {0}.", this.item.SerializedType);
			this.buffer.AddRange(this.deliveryHandler.Deliver(this.item, live));

			// deliveryHandler ensures that messages are gathered from the hydratables only once we reach the live stream
			for (var i = 0; i < this.buffer.Count; i++)
			{
				Log.Debug("Now transforming additional hydratables that subscribe to messages generated by original and subsequent transformation.");
				this.buffer.AddRange(this.deliveryHandler.Deliver(this.buffer[i], this.item.MessageSequence + i + 1));
			}

			return live;
		}
		private void PublishToJournalPhase()
		{
			Log.DebugFormat("Publishing {0} items to the Journal Disruptor.", this.buffer.Count + this.offset);

			var size = this.buffer.Count + this.offset;
			if (size == 0)
				return;

			var batch = this.journalRing.Next(size);

			if (this.offset > 0)
				this.journalRing[batch.Start].AsForeignMessage(
					this.currentSequnce + 1, this.item.SerializedBody, this.item.Body, this.item.Headers, this.item.ForeignId, this.item.Acknowledgment);

			for (var i = this.offset; i < size; i++)
				this.journalRing[i + batch.Start].AsTransformationResultMessage(this.currentSequnce + 1 + i, this.buffer[i - this.offset], BlankHeaders);

			this.journalRing.Publish(batch);
		}
		private void Increment()
		{
			if (this.item.MessageSequence > this.currentSequnce)
			{
				this.currentSequnce += this.buffer.Count + this.offset;
				this.snapshot.Track(this.currentSequnce);
			}

			this.offset = 0;
			this.buffer.Clear();
		}

		public TransformationHandler(
			long journaledSequence, 
			IRingBuffer<JournalItem> journalRing,
			IDeliveryHandler deliveryHandler,
			ISystemSnapshotTracker snapshot)
		{
			if (journaledSequence < 0)
				throw new ArgumentOutOfRangeException("journaledSequence");

			if (journalRing == null)
				throw new ArgumentNullException("journalRing");

			if (deliveryHandler == null)
				throw new ArgumentNullException("deliveryHandler");

			if (snapshot == null)
				throw new ArgumentNullException("snapshot");

			this.currentSequnce = journaledSequence;
			this.journalRing = journalRing;
			this.deliveryHandler = deliveryHandler;
			this.snapshot = snapshot;
		}

		private const int IncludeIncomingMessage = 1;
		private static readonly ILog Log = LogManager.GetLogger(typeof(TransformationHandler));
		private static readonly Dictionary<string, string> BlankHeaders = new Dictionary<string, string>(); 
		private readonly IRingBuffer<JournalItem> journalRing;
		private readonly IDeliveryHandler deliveryHandler;
		private readonly ISystemSnapshotTracker snapshot;
		private readonly List<object> buffer = new List<object>();

		private TransformationItem item;
		private long currentSequnce;
		private int offset;
		private bool skipAllRemaining;
	}
}