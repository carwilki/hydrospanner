namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(TransformationItem message);
		IEnumerable<object> Handle(object message, long currentSequence);
	}

	public class Transformer : ITransformer
	{
		public IEnumerable<object> Handle(TransformationItem message)
		{
			yield break;
		}

		public IEnumerable<object> Handle(object message, long currentSequence)
		{
			yield break;
		}

		public Transformer(long journaledSequence, IRingBuffer<SnapshotItem> snapshotRing, IRepository repository)
		{
		}
	}
}