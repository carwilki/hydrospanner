namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Serialization;

	internal class DeserializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			data.Deserialize(this.serializer);
		}

		public DeserializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly ISerializer serializer;
	}
}