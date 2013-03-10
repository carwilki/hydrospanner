﻿namespace Hydrospanner
{
	using System.Collections.Generic;
	using Disruptor;

	public class TransformationHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.DuplicateMessage)
				return;

			if (data.MessageSequence == 0)
				data.MessageSequence = ++this.currentSequence;

			this.Hydrate(data);
			this.PublishMessages(data);
			this.SaveSnapshot(data);
		}
		private void Hydrate(WireMessage data)
		{
			foreach (var key in this.selector.Keys(data.Body, data.Headers))
			{
				var hydratable = this.LoadHydratable(key);
				if (hydratable == null)
					continue;

				hydratable.Hydrate(data.Body, data.Headers, data.LiveMessage);

				var complete = hydratable.IsComplete;
				if (!data.LiveMessage && !complete)
					continue; // replay mode and there's more to do.

				this.gathered.AddRange(hydratable.GatherMessages() ?? new object[0]);

				if (!complete)
					continue;

				// this.tombstone.Add(key);
				this.repository[key.Name] = null;
			}
		}
		private IHydratable LoadHydratable(IHydratableKey key)
		{
			IHydratable hydratable;
			if (this.repository.TryGetValue(key.Name, out hydratable))
				return hydratable; // this might be null when a given hydratable has been completed

			// TODO: it doesn't exist in the repo, check the tombstone/completed collection
			return this.repository[key.Name] = key.Create();
		}
		private void PublishMessages(WireMessage data)
		{
			var forwardIncomingMessage = data.MessageSequence == 0;
			var batchSize = this.gathered.Count + (forwardIncomingMessage ? 1 : 0);
			if (batchSize == 0)
				return;

			var batch = this.dispatch.NewBatchDescriptor(batchSize);
			if (forwardIncomingMessage)
			{
				var item = this.dispatch[batch.Start];
				item.Clear();
				item.MessageSequence = data.MessageSequence;
				item.Body = data.Body;
				item.Headers = data.Headers;
				item.SerializedBody = data.SerializedBody;
				item.SerializedHeaders = data.SerializedHeaders;
				item.WireId = data.WireId;
				item.AcknowledgeDelivery = data.AcknowledgeDelivery;
			}

			for (var i = 1; i < batchSize; i++)
			{
				var pending = this.gathered[i - 1];
				var item = this.dispatch[batch.Start + i];
				item.Clear();
				item.MessageSequence = ++this.currentSequence;
				item.Body = pending;
				item.Headers = new Dictionary<string, string>(); // TODO: where do these come from???
			}

			this.dispatch.Publish(batch);
			this.gathered.Clear();
		}
		private void SaveSnapshot(WireMessage data)
		{
			if (data.AcknowledgeDelivery != null)
				return; // message is coming in from the wire, don't do anything

			if (this.journaledMessagesSinceLastSnapshot++ % this.snapshotFrequency != 0)
				return; // not enough messages have occurred since the last snapshot

			var published = 0;
			var count = this.repository.Count;

			foreach (var hydratable in this.repository.Values)
			{
				var claimed = this.snapshot.Next();
				var item = this.snapshot[claimed];
				item.Clear();
				item.CurrentSequence = data.MessageSequence;
				item.Memento = hydratable.GetMemento();
				item.MementosRemaining = count - ++published; // off by one?
				this.snapshot.Publish(claimed);
			}
		}

		public TransformationHandler(
			Dictionary<string, IHydratable> repository,
			RingBuffer<DispatchMessage> dispatch,
			RingBuffer<SnapshotMessage> snapshot,
			int snapshotFrequency,
			IHydratableSelector selector,
			long currentSequence)
		{
			this.repository = repository;
			this.dispatch = dispatch;
			this.snapshot = snapshot;
			this.snapshotFrequency = snapshotFrequency;
			this.selector = selector;
			this.currentSequence = currentSequence;
		}

		private readonly List<object> gathered = new List<object>();
		private readonly Dictionary<string, IHydratable> repository;
		private readonly RingBuffer<SnapshotMessage> snapshot;
		private readonly RingBuffer<DispatchMessage> dispatch;
		private readonly int snapshotFrequency;
		private readonly IHydratableSelector selector;
		private int journaledMessagesSinceLastSnapshot;
		private long currentSequence;
	}
}