namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using System.Linq;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live);
	}

	public class Transformer : ITransformer
	{
		// TODO: delete and TDD; this is just spike code to see if I've got the concepts down...

		public IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live)
		{
			this.currentSequence++;

			var hydratables = this.repository.Load(message, headers);

			foreach (var h in hydratables.Where(h => !h.IsComplete))
			{
				h.Hydrate(message, headers, live);

				if (h.IsPublicSnapshot || h.IsComplete)
				{
					var next = this.snapshotRing.Next();
					var claimed = this.snapshotRing[next];
					claimed.AsPublicSnapshot(h.Key, h.GetMemento(), this.currentSequence);
					this.snapshotRing.Publish(next);
					
					if (h.IsComplete) // Not sure about this...
						this.repository.Delete(h);
				}

				foreach (var m in h.GatherMessages())
					yield return m;
			}
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing, long journaledSequence)
		{
			this.snapshotRing = snapshotRing;
			this.currentSequence = journaledSequence + 1;
			this.repository = repository;
		}

		readonly IRepository repository;
		readonly IRingBuffer<SnapshotItem> snapshotRing;
		long currentSequence;
	}
}