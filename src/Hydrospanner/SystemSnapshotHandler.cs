namespace Hydrospanner
{
	using Disruptor;

	public sealed class SystemSnapshotHandler : IEventHandler<SnapshotMessage>
	{
		public void OnNext(SnapshotMessage data, long sequence, bool endOfBatch)
		{
			if (data.IsolatedSnapshot)
				return;

			if (data.MementosRemaining != this.remaining--)
			{
				this.remaining = data.MementosRemaining;

				if (this.stream != null)
					this.stream.Dispose();

				this.stream = this.snapshotter.Create(data.CurrentSequence, data.MementosRemaining);
			}

			this.stream.WriteItem(data.Serialized);
			if (this.remaining != 0)
				return;

			this.stream.Dispose();
			this.stream = null;
			this.remaining = 0;
		}

		public SystemSnapshotHandler(SystemSnapshotter snapshotter)
		{
			this.snapshotter = snapshotter;
		}

		private readonly SystemSnapshotter snapshotter;
		private SnapshotOutputStream stream;
		private long remaining;
	}
}