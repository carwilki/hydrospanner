﻿namespace Hydrospanner
{
	using System.Collections.Generic;
	using Disruptor;

	public sealed class TransformationHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.DuplicateMessage)
				return;

			var reachedLive = !this.live && data.LiveMessage;

			if (data.LiveMessage)
				data.MessageSequence = ++this.currentSequence;

			this.live = this.live || reachedLive; // TODO: once we've reach the live stream, we may want to consider doing a snapshot among other things

			this.Hydrate(data);

			if (endOfBatch)
				this.SaveSystemSnapshot(data);
		}
		private void Hydrate(WireMessage data)
		{
			foreach (var key in this.selector.Keys(data.Body, data.Headers))
			{
				var hydratable = this.LoadHydratable(key);
				if (hydratable == null)
					continue;

				hydratable.Hydrate(data.Body, data.Headers, data.LiveMessage);

				if (data.LiveMessage)
				{
					data.DispatchMessages.AddRange(hydratable.GatherMessages() ?? new object[0]);
					this.currentSequence += data.DispatchMessages.Count;
				}

				if (hydratable.IsComplete || (hydratable.SnapshotFrequency > 0 && data.MessageSequence % hydratable.SnapshotFrequency == 0))
				{
					// TODO: capture individual "item" snapshot (same state as "system" snapshot, but pushed to snapshot ring and typically used for projections)
					// for aggregates/sagas which are complete, it returns null
					// for a projection, it returns the projection payload
					// snapshots also have the interesting property of "last one wins" which means that if 100K updates occur, we only need the last one
					// and if saving to disk is slow, only the last one will be saved during each disk write.
				}

				if (!hydratable.IsComplete)
					continue;

				// TODO; large tombstone until we reach live at which point it contracts to a smaller size?
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
		private void SaveSystemSnapshot(WireMessage data)
		{
			// TODO: this method needs to consider what will happen when we reach the live stream
			// for example, if the last snapshot was over X messages ago and we reach the live stream
			// we may decide to perform a system-wide snapshot

			if (data.LiveMessage)
				return; // message is coming in from the wire, don't do anything

			if (this.currentSequence % this.snapshotFrequency != 0)
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
			RingBuffer<SnapshotMessage> snapshot,
			int snapshotFrequency,
			IHydratableSelector selector,
			long currentSequence)
		{
			this.repository = repository;
			this.snapshot = snapshot;
			this.snapshotFrequency = snapshotFrequency;
			this.selector = selector;
			this.currentSequence = currentSequence;
		}

		private readonly Dictionary<string, IHydratable> repository;
		private readonly RingBuffer<SnapshotMessage> snapshot;
		private readonly int snapshotFrequency;
		private readonly IHydratableSelector selector;
		private long currentSequence;
		private bool live;
	}
}