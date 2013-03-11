namespace Hydrospanner
{
	using Disruptor;

	public sealed class SystemSnapshotHandler : IEventHandler<SnapshotMessage>
	{
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			if (data.IsolatedSnapshot)
				return;

			if (data.MementosRemaining == 0 || data.MementosRemaining != this.remaining--)
			{
				this.remaining = data.MementosRemaining;

				if (this.stream != null)
					this.stream.Dispose();

				this.stream = this.recorder.Create(data.CurrentSequence, data.MementosRemaining + 1);
			}

			this.stream.WriteItem(data.Serialized);
			if (this.remaining != 0)
				return;

			this.stream.Dispose();
			this.stream = null;
			this.remaining = 0;
		}

		public SystemSnapshotHandler(SystemSnapshotRecorder recorder)
		{
			this.recorder = recorder;
		}

		private readonly SystemSnapshotRecorder recorder;
		private SnapshotOutputStream stream;
		private long remaining;
	}
}