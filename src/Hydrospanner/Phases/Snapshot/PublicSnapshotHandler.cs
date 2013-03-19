namespace Hydrospanner.Phases.Snapshot
{
	using System.Collections.Generic;
	using Disruptor;

	internal class PublicSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (data.IsPublicSnapshot)
				this.buffer[data.Key] = data;

			if (endOfBatch && this.buffer.Count > 0)
				this.RecordPublicSnapshots();
		}

		private void RecordPublicSnapshots()
		{
			this.recorder.StartRecording(this.buffer.Count);

			foreach (var item in this.buffer)
				this.recorder.Record(item.Value);

			this.recorder.FinishRecording();

			this.buffer.Clear();
		}

		public PublicSnapshotHandler(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		readonly ISnapshotRecorder recorder;
		readonly Dictionary<string, SnapshotItem> buffer = new Dictionary<string, SnapshotItem>();
	}
}