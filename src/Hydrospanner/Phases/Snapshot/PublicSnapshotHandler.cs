namespace Hydrospanner.Phases.Snapshot
{
	using System.Collections.Generic;
	using Disruptor;
	using log4net;

	internal class PublicSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (data.IsPublicSnapshot)
			{
				Log.DebugFormat(
					"Receiving public SnapshotItem at location {0}, current message sequence is {1}.",
					data.Key,
					data.CurrentSequence);

				this.buffer[data.Key] = data;
			}

			if (endOfBatch && this.buffer.Count > 0)
				this.RecordPublicSnapshots();
		}

		private void RecordPublicSnapshots()
		{
			Log.InfoFormat("Persisting {0} public snapshot items to disk.", this.buffer.Count);

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

		private static readonly ILog Log = LogManager.GetLogger(typeof(PublicSnapshotHandler));
		private readonly ISnapshotRecorder recorder;
		private readonly Dictionary<string, SnapshotItem> buffer = new Dictionary<string, SnapshotItem>();
	}
}