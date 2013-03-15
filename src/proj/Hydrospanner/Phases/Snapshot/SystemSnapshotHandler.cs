﻿namespace Hydrospanner.Phases.Snapshot
{
	using Disruptor;

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

			this.recorder.Record(data);

			if (data.MementosRemaining == 0)
				this.FinishRecording(data);
		}

		private void StartRecording(SnapshotItem data)
		{
			this.recorder.StartRecording(data.MementosRemaining + 1);
			this.recording = true;
		}

		private void FinishRecording(SnapshotItem data)
		{
			this.recorder.FinishRecording(this.latestIteration++, data.CurrentSequence);
			this.recording = false;
		}

		public SystemSnapshotHandler(ISnapshotRecorder recorder, int latestIteration)
		{
			this.recorder = recorder;
			this.latestIteration = latestIteration;
		}

		private readonly ISnapshotRecorder recorder;
		private int latestIteration;
		private bool recording;
	}
}