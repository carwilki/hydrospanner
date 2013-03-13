namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Hydrospanner.Serialization;

	public class DeserializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			data.Deserialize(this.serializer);
		}

		public DeserializationHandler(JsonSerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly JsonSerializer serializer;
	}
}