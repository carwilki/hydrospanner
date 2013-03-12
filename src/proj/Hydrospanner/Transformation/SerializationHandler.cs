namespace Hydrospanner.Transformation
{
	using Disruptor;

	public class SerializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
		}
	}
}