namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;

	public sealed class TransformationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			// TODO: perform de-duplication here, e.g. if the item is duplicate, just forward the item
			// to the next ring as an "ack-only" message and then return
		}
	}
}