namespace Hydrospanner.Timeout
{
	using System;
	using System.Threading;
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

				this.timer = new Timer(this.OnTimeout, null, StartNow, OncePerSecond); // once per second
			}
		}
		private void OnTimeout(object state)
		{
			var sequence = this.ring.Next();
			var item = this.ring[sequence];
			item.AsTransientMessage(new CurrentTimeMessage(SystemTime.UtcNow));
			this.ring.Publish(sequence);
		}

		public SystemClock(IRingBuffer<TransformationItem> ring) : this()
		{
			this.ring = ring;
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
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.timer = this.timer.TryDispose();
		}

		private const int OncePerSecond = 1000;
		private const int StartNow = 0;
		private readonly object sync = new object();
		private readonly IRingBuffer<TransformationItem> ring;
		private Timer timer;
		private bool disposed;
	}
}