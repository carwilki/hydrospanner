namespace Hydrospanner.Phases
{
	using Bootstrap;
	using Disruptor;
	using Journal;
	using Snapshot;
	using Transformation;

	public class ClearItemHandler : IEventHandler<TransformationItem>, IEventHandler<SnapshotItem>, IEventHandler<JournalItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			data.Clear();
		}
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			data.Clear();
		}
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			data.Clear();
		}
	}
}