namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;

	public class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
		}
	}
}