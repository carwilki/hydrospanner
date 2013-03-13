namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;

	public class SystemSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (!data.IsPublicSnapshot)
				this.recorder.Record(data);
		}

		public SystemSnapshotHandler(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		readonly ISnapshotRecorder recorder;
	}
}