namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(TransformationItem message);
		IEnumerable<object> Handle(object message, long currentSequence);
	}

	public interface ISnapshotTracker
	{
		void Increment(int messages);
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

	public class SnapshotTracker : ISnapshotTracker
	{
		public void Increment(int messages)
		{
		}

		public SnapshotTracker(long journaledSequence, IRingBuffer<SnapshotItem> snapshotRing, IRepository repository)
		{
		}
	}
}