namespace Hydrospanner.Timeout
{
	using System;
	using Phases.Transformation;

	public class SystemClock : IDisposable
	{
		public virtual void Start()
		{
			lock (this.sync)
			{
				if (this.disposed)
					return;

				if (this.timer != null)
					return;

				this.timer = this.timerBuilder();
				this.timer.Start(this.OnTimeout);
			}
		}
		private void OnTimeout(object state)
		{
			var sequence = this.ring.Next();
			var item = this.ring[sequence];
			item.AsTransientMessage(new CurrentTimeMessage(SystemTime.UtcNow));
			this.ring.Publish(sequence);
		}

		public SystemClock(IRingBuffer<TransformationItem> ring, Func<TimerWrapper> timerBuilder) : this()
		{
			this.ring = ring;
			this.timerBuilder = timerBuilder;
		}
		protected SystemClock()
		{
		}
		
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
				this.timer = this.timer.TryDispose();

			this.disposed = true;
		}

		private readonly object sync = new object();
		private readonly IRingBuffer<TransformationItem> ring;
		private readonly Func<TimerWrapper> timerBuilder;
		private TimerWrapper timer;
		private bool disposed;
	}
}