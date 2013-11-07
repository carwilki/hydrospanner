namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using log4net;
	using Transformation;

	public sealed class CountdownHandler : IEventHandler<BootstrapItem>, IEventHandler<TransformationItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Countdown at {0}, receiving bootstrap item.", this.countdown);

			if (!this.failed && data != null && data.Memento == null && data.SerializedMemento != null)
				this.failed = true;

			if (--this.countdown == 0)
			{
				Log.InfoFormat("Successfully restored {0} mementos from snapshot", this.items);
				this.callback(!this.failed);
			}
		}
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Countdown at {0}, receiving transformation item.", this.countdown);

			this.failed = this.failed || (data != null && data.Body == null);
			if (--this.countdown == 0)
			{
				Log.InfoFormat("Successfully replayed {0} messages from journal.", this.items);

				if (this.failed)
					Log.Error("Some messages could not be replayed.");

				this.callback(!this.failed);
			}
		}

		public CountdownHandler(long countdown, Action<bool> callback)
		{
			if (countdown <= 0)
				throw new ArgumentOutOfRangeException("countdown");

			if (callback == null)
				throw new ArgumentNullException("callback");

			this.items = countdown;
			this.countdown = countdown;
			this.callback = callback;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(CountdownHandler));
		private readonly Action<bool> callback;
		private readonly long items;
		private long countdown;
		private bool failed;
	}
}