namespace Hydrospanner.Phases.Bootstrap
{
	using Disruptor;
	using Hydrospanner.Serialization;

	public class SerializationHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			// TODO: get this under test
			data.Memento = this.serializer.Deserialize(data.SerializedMemento, data.SerializedType); // TODO
		}

		public SerializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		private readonly ISerializer serializer;
	}
}