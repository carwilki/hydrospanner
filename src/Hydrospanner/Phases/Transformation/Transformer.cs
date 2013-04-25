namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Snapshot;
	using Timeout;

	public sealed class Transformer : ITransformer
	{
		public IEnumerable<object> Transform<T>(Delivery<T> delivery)
		{
			this.gathered.Clear();

			foreach (var hydratable in this.repository.Load(delivery))
			{
				hydratable.Hydrate(delivery);
				this.GatherState(delivery.Live, delivery.Sequence, hydratable);
			}

			return this.gathered;
		}
		private void GatherState(bool live, long messageSequence, IHydratable hydratable)
		{
			if (live)
			{
				this.AddMessages(hydratable);
				this.watcher.Add(hydratable as ITimeoutHydratable);
			}
				
			if (hydratable.IsPublicSnapshot || hydratable.IsComplete)
				this.TakeSnapshot(hydratable, messageSequence);

			if (!hydratable.IsComplete)
				return;

			this.repository.Delete(hydratable);

			if (live)
				this.watcher.Remove(hydratable.Key);
		}
		private void AddMessages(IHydratable hydratable)
		{
			var messages = hydratable.PendingMessages;
			this.gathered.AddRange(messages);
			if (!messages.IsReadOnly)
				messages.Clear();
		}
		private void TakeSnapshot(IHydratable hydratable, long messageSequence)
		{
			var memento = hydratable.Memento;
			var cloner = memento as ICloneable;
			memento = (cloner == null ? memento : cloner.Clone()) ?? memento;

			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsPublicSnapshot(hydratable.Key, memento, messageSequence);
			this.ring.Publish(next);
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> ring, ITimeoutWatcher watcher)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (ring == null)
				throw new ArgumentNullException("ring");

			if (watcher == null)
				throw new ArgumentNullException("watcher");

			this.repository = repository;
			this.ring = ring;
			this.watcher = watcher;
		}

		private readonly List<object> gathered = new List<object>();
		private readonly IRingBuffer<SnapshotItem> ring;
		private readonly IRepository repository;
		private readonly ITimeoutWatcher watcher;
	}
}