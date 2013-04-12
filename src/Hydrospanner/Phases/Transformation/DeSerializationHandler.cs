namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using log4net;
	using Serialization;

	public sealed class DeserializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Deserializing transformation item of type {0}.", data.SerializedType);

			data.Deserialize(this.serializer);
		}

		public DeserializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(DeserializationHandler));
		private readonly ISerializer serializer;
	}
}