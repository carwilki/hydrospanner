namespace Hydrospanner.Timeout
{
	using System;
	using System.Threading;

	public class SystemTimer : IDisposable
	{
		public virtual void Start(TimerCallback callback)
		{
			this.timer = new Timer(callback, null, StartImmediately, this.interval.Milliseconds);
		}

		public SystemTimer(TimeSpan interval)
		{
			this.interval = interval;
		}
		protected SystemTimer()
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

		private const int StartImmediately = 0;
		private readonly TimeSpan interval;
		private bool disposed;
		private Timer timer;
	}
}