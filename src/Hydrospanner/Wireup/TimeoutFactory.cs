namespace Hydrospanner.Wireup
{
	using System;
	using Phases.Transformation;
	using Timeout;

	public class TimeoutFactory
	{
		public virtual SystemClock CreateSystemClock(IRingBuffer<TransformationItem> ring)
		{
			return new SystemClock(ring, () => new TimerWrapper(TimeSpan.FromSeconds(1)));
		}
	}
}