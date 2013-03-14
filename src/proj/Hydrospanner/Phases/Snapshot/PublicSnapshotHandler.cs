namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;

	internal class PublicSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (data.IsPublicSnapshot)
				this.recorder.Record(data);
		}

		public PublicSnapshotHandler(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		readonly ISnapshotRecorder recorder;
	}
}