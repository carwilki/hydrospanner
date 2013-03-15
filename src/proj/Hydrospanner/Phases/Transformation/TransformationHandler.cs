namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;

	public sealed class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
		}
	}
}