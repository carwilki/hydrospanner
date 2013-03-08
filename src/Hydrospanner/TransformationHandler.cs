namespace Hydrospanner
{
	using System.Collections.Generic;
	using Disruptor;

	public class TransformationHandler : IEventHandler<WireMessage>
	{
		public void AddHydratable()
		{
			// TODO: when loading from snapshot
		}

		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			var keys = this.selector.Keys(data.Body, data.Headers);

			for (var i = 0; i < keys.Length; i++)
			{
				var hydratable = this.LoadHydratable(keys[i], data.Body);
				if (hydratable == null)
					continue;

				// TODO: hydrate

				// if (hydratable.IsComplete) // TODO: "complete" handling here... as necessary
				if (data.Replay)
					continue;

				this.gathered.AddRange(hydratable.GatherMessages());
			}

			this.PublishGatheredMessages();
			this.SaveSnapshot(data.MessageSequence);
		}
		private IHydratable LoadHydratable(string key, object message)
		{
			IHydratable hydratable;
			if (this.repository.TryGetValue(key, out hydratable))
				return hydratable; // this might be null when a given hydratable has been completed

			// TODO: it doesn't exist in the repo, check the tombstone/completed collection
			return this.repository[key] = this.selector.Create(key, message);
		}
		private void PublishGatheredMessages()
		{
			// TODO: incoming message may have to be published as well (e.g. wire = event + needs to be journaled AND acked).
			// but how does the Hydrospanner know?

			if (this.gathered.Count == 0)
				return;

			var batch = this.dispatch.NewBatchDescriptor(this.gathered.Count);
			batch = this.dispatch.Next(batch);

			for (var i = 0; i < this.gathered.Count; i++)
			{
				var pending = this.gathered[i];

				var item = this.dispatch[i + batch.Start];
				item.Clear();
				item.Body = pending;
			}

			this.dispatch.Publish(batch);
			this.gathered.Clear();
		}
		private void SaveSnapshot(long sequence)
		{
			if (sequence == 0)
				return;

			if (this.journaledMessagesSinceLastSnapshot++ % this.snapshotFrequency != 0)
				return;

			var published = 0;
			var count = this.repository.Count;

			foreach (var hydratable in this.repository.Values)
			{
				var claimed = this.snapshot.Next();
				var item = this.snapshot[claimed];
				item.Clear();
				item.CurrentSequence = sequence;
				item.Memento = hydratable.GetMemento();
				item.MementosRemaining = count - published++; // off by one?
				this.snapshot.Publish(claimed);
			}
		}

		public TransformationHandler(
			RingBuffer<DispatchMessage> dispatch,
			RingBuffer<SnapshotMessage> snapshot,
			int snapshotFrequency,
			IHydratableSelector selector)
		{
			this.dispatch = dispatch;
			this.snapshot = snapshot;
			this.snapshotFrequency = snapshotFrequency;
			this.selector = selector;
		}

		private readonly Dictionary<string, IHydratable> repository = new Dictionary<string, IHydratable>(); // TODO: set initial capacity?
		private readonly List<object> gathered = new List<object>();
		private readonly RingBuffer<SnapshotMessage> snapshot;
		private readonly RingBuffer<DispatchMessage> dispatch;
		private readonly int snapshotFrequency;
		private readonly IHydratableSelector selector;
		private int journaledMessagesSinceLastSnapshot;
		private int completedHydratableCounter;
	}
}