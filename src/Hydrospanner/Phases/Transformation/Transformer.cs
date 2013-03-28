namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live);
	}

	public sealed class Transformer : ITransformer
	{
		// TODO: delete and TDD; this is just spike code to see if I've got the concepts down...

		public IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live)
		{
			this.gathered.Clear();

			// TODO: perhaps we should make the handle method receive the current sequence again?
			// then we can compare it to the journaled sequence to see if we are live;
			// I also noticed that the snapshot tracker needs the current sequence and is tracking
			// that internally; would it be better to pass it in there as well?
			this.currentSequence++;

			foreach (var hydratable in this.repository.Load(message, headers))
			{
				hydratable.Hydrate(message, headers, live);

				if (live)
					this.gathered.AddRange(hydratable.GatherMessages());

				if (hydratable.IsPublicSnapshot || hydratable.IsComplete)
					this.TakeSnapshot(hydratable);

				if (hydratable.IsComplete)
					this.repository.Delete(hydratable); // this ensures that completed hydratables are never returned during the load phase
			}

			return this.gathered;
		}
		private void TakeSnapshot(IHydratable hydratable)
		{
			var next = this.snapshotRing.Next();
			var claimed = this.snapshotRing[next];
			claimed.AsPublicSnapshot(hydratable.Key, hydratable.GetMemento(), this.currentSequence);
			this.snapshotRing.Publish(next);
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing, long journaledSequence)
		{
			this.snapshotRing = snapshotRing;
			this.currentSequence = journaledSequence + 1;
			this.repository = repository;
		}

		private readonly List<object> gathered = new List<object>();
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
		private long currentSequence;
	}
}