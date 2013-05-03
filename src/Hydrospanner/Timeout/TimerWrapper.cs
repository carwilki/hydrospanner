namespace Hydrospanner.Timeout
{
	using System;
	using System.Threading;

	public class TimerWrapper : IDisposable
	{
		public virtual void Start(TimerCallback callback)
		{
			this.timer = new Timer(callback, null, StartImmediately, (int)this.interval.TotalMilliseconds);
		}

		public TimerWrapper(TimeSpan interval)
		{
			this.interval = interval;
		}
		protected TimerWrapper()
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