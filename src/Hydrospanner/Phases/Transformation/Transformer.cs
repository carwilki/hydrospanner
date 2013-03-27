namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;
	using Snapshot;

	public interface ITransformer
	{
		IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live);
	}

	public class Transformer : ITransformer
	{
		public IEnumerable<object> Handle(object message, Dictionary<string, string> headers, bool live)
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