namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Phases.Transformation;

	public class SystemClock : IDisposable
	{
		public void Start()
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
			item.AsLocalMessage(0, new CurrentTimeMessage(SystemTime.UtcNow), EmptyHeaders);
			this.ring.Publish(sequence);
		}

		public SystemClock(IRingBuffer<TransformationItem> ring)
		{
			this.ring = ring;
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
		private static readonly Dictionary<string, string> EmptyHeaders = new Dictionary<string, string>();
		private readonly object sync = new object();
		private readonly IRingBuffer<TransformationItem> ring;
		private Timer timer;
		private bool disposed;
	}
}