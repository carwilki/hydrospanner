﻿namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using Hydrospanner.Phases.Transformation;

	public class TrackingHandler : IEventHandler<BootstrapItem>, IEventHandler<TransformationItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			if (--this.countdown == 0)
				this.callback();
		}
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			if (--this.countdown == 0)
				this.callback();
		}

		public TrackingHandler(long countdown, Action callback)
		{
			if (countdown <= 0)
				throw new ArgumentOutOfRangeException("countdown");

			if (callback == null)
				throw new ArgumentNullException("callback");

			this.countdown = countdown;
			this.callback = callback;
		}

		private readonly Action callback;
		private long countdown;
	}
}