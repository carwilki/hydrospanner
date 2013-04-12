namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;
	using log4net;

	internal class SystemSnapshotHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (!data.IsPublicSnapshot)
				this.Record(data);
		}

		private void Record(SnapshotItem data)
		{
			if (!this.recording)
				this.StartRecording(data);

			this.RecordItem(data);

			if (data.MementosRemaining == 0)
				this.FinishRecording(data);
		}

		private void StartRecording(SnapshotItem data)
		{
			Log.InfoFormat(
				"Recording started for system snapshot at message sequence {0} with {1} items",
				data.CurrentSequence,
				data.MementosRemaining + 1);
			this.recorder.StartRecording(data.MementosRemaining + 1);
			this.recording = true;
		}

		private void RecordItem(SnapshotItem data)
		{
			Log.DebugFormat("Recording system snapshot item at message sequence {0}. {1} items remaining in snapshot.",
				data.CurrentSequence,
				data.MementosRemaining);

			this.recorder.Record(data);
		}

		private void FinishRecording(SnapshotItem data)
		{
			this.recorder.FinishRecording(data.CurrentSequence);
			this.recording = false;
			Log.InfoFormat("Recording finished for system snapshot at message sequence {0}.", data.CurrentSequence);
		}

		public SystemSnapshotHandler(ISnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SystemSnapshotHandler));
		private readonly ISnapshotRecorder recorder;
		private bool recording;
	}
}