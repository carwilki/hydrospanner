namespace Hydrospanner.Wireup
{
	using Phases.Transformation;
	using Timeout;

	public class TimeoutFactory
	{
		public virtual SystemClock CreateSystemClock(IRingBuffer<TransformationItem> ring)
		{
			return new SystemClock(ring);
		}
	}
}