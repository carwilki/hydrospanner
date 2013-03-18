namespace Hydrospanner.Phases.Snapshot
{
	using System.Collections.Generic;
	using Disruptor;

	internal class PublicSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (data.IsPublicSnapshot)
				this.buffer.Enqueue(data); // TODO: replace older item with same key with new item

			if (endOfBatch && this.buffer.Count > 0)
				this.RecordPublicSnapshots();
		}

		private void RecordPublicSnapshots()
		{
			this.recorder.StartRecording(this.buffer.Count);

			while (this.buffer.Count > 0)
				this.recorder.Record(this.buffer.Dequeue());

			this.recorder.FinishRecording();
		}

		public PublicSnapshotHandler(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		readonly ISnapshotRecorder recorder;
		readonly Queue<SnapshotItem> buffer = new Queue<SnapshotItem>();
	}
}