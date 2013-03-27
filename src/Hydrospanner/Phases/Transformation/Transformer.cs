namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(TransformationItem message, bool currentSequence);
		IEnumerable<object> Handle(object message, bool live);
	}

	public class Transformer : ITransformer
	{
		public IEnumerable<object> Handle(TransformationItem message, bool live)
		{
			yield break;
		}

		public IEnumerable<object> Handle(object message, bool live)
		{
			yield break;
		}

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing)
		{
			this.snapshotRing = snapshotRing;
			this.repository = repository;
		}

		readonly IRepository repository;
		readonly IRingBuffer<SnapshotItem> snapshotRing;
	}
}