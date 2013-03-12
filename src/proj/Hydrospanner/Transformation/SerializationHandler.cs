namespace Hydrospanner.Transformation
{
	using Disruptor;

	public class SerializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			data.Deserialize(this.serializer);
		}

		public SerializationHandler(JsonSerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly JsonSerializer serializer;
	}
}