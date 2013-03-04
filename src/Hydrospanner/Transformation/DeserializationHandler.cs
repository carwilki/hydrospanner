namespace Hydrospanner.Transformation
{
	using Disruptor;

	public class DeserializationHandler : IEventHandler<TransformationMessage>
	{
		public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
			// deserialize anything that hasn't already been deserialized.
		}
	}
}