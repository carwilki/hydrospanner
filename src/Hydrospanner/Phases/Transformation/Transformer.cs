namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Snapshot;

	public sealed class Transformer : ITransformer
	{
		public IEnumerable<object> Transform<T>(Delivery<T> delivery)
		{
			this.gathered.Clear();

			// determine if this is the first live message

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
				var messages = hydratable.PendingMessages;
				this.gathered.AddRange(messages);
				messages.Clear();
			}
				
			if (hydratable.IsPublicSnapshot || hydratable.IsComplete)
				this.TakeSnapshot(hydratable, messageSequence);

			if (hydratable.IsComplete)
				this.repository.Delete(hydratable); // TODO: remove any timeouts (if ITimeoutHydratable)

			// if this is the *first* live message
			// enumerate over every single hydratable in the repository...
			// for each IAlarmHydratable, get the set of date/time timeouts requested
			// and add those the timeout hydratable (which is referenced right here)
		}
		private void TakeSnapshot(IHydratable hydratable, long messageSequence)
		{
			var memento = hydratable.GetMemento();
			var cloner = memento as ICloneable;
			memento = (cloner == null ? memento : cloner.Clone()) ?? memento;

			var next = this.snapshotRing.Next();
			var claimed = this.snapshotRing[next];
			claimed.AsPublicSnapshot(hydratable.Key, memento, messageSequence);
			this.snapshotRing.Publish(next);
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (snapshotRing == null)
				throw new ArgumentNullException("snapshotRing");

			this.repository = repository;
			this.snapshotRing = snapshotRing;
		}

		private readonly List<object> gathered = new List<object>();
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
	}
}