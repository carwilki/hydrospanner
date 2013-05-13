namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using log4net;
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
				this.AddMessages(hydratable);
			else if (hydratable.PendingMessages.Count > 0)
			{
				Log.WarnFormat("Hydratable at '{0}' has {1} pending messages during replay, but shouldn't.", hydratable.Key, hydratable.PendingMessages.Count);
				hydratable.PendingMessages.TryClear();
			}

			var @public = hydratable as IPublicHydratable;
			if (@public != null && (live || hydratable.IsComplete))
				this.TakePublicSnapshot(@public, messageSequence);

			if (!hydratable.IsComplete)
				return;

			this.repository.Delete(hydratable);

			if (live)
				this.AddMessages(this.watcher.Abort(hydratable));
		}
		private void AddMessages(IHydratable hydratable)
		{
			var key = hydratable.Key;
			var messages = hydratable.PendingMessages;
			foreach (var message in messages)
				this.gathered.Add(this.watcher.Filter(key, message));

			messages.TryClear();
		}
		private void TakePublicSnapshot(IPublicHydratable hydratable, long messageSequence)
		{
			var memento = hydratable.Memento;
			var cloner = memento as ICloneable;
			memento = (cloner == null ? memento : cloner.Clone()) ?? memento;

			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsPublicSnapshot(hydratable.Key, memento, hydratable.MementoType, messageSequence);
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

		private static readonly ILog Log = LogManager.GetLogger(typeof(Transformer));
		private readonly List<object> gathered = new List<object>();
		private readonly IRingBuffer<SnapshotItem> ring;
		private readonly IRepository repository;
		private readonly ITimeoutWatcher watcher;
	}
}