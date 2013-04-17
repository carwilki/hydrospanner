namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Snapshot;

	public sealed class Transformer : ITransformer
	{
		public IEnumerable<object> Handle(object message, Dictionary<string, string> headers, long sequence)
		{
			this.gathered.Clear();

			var live = sequence > this.journaledSequence;
			foreach (var hydratable in this.repository.Load(message, headers))
			{
				hydratable.Hydrate(message, headers, live);
				this.GatherState(live, sequence, hydratable);
			}

			return this.gathered;
		}
		private void GatherState(bool live, long messageSequence, IHydratable hydratable)
		{
			if (live)
				this.gathered.AddRange(hydratable.GatherMessages());
				
			if (hydratable.IsPublicSnapshot || hydratable.IsComplete)
				this.TakeSnapshot(hydratable, messageSequence);

			if (hydratable.IsComplete)
				this.repository.Delete(hydratable);
		}
		private void TakeSnapshot(IHydratable hydratable, long messageSequence)
		{
			var next = this.snapshotRing.Next();
			var claimed = this.snapshotRing[next];
			claimed.AsPublicSnapshot(hydratable.Key, hydratable.GetMemento(), messageSequence);
			this.snapshotRing.Publish(next);
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing, long journaledSequence)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (snapshotRing == null)
				throw new ArgumentNullException("snapshotRing");

			if (journaledSequence < 0)
				throw new ArgumentOutOfRangeException("journaledSequence");

			this.repository = repository;
			this.snapshotRing = snapshotRing;
			this.journaledSequence = journaledSequence;
		}

		private readonly List<object> gathered = new List<object>();
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
		private readonly long journaledSequence;
	}
}