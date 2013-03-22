namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using Serialization;

	public class SerializationHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			data.Memento = this.serializer.Deserialize(data.SerializedMemento, data.SerializedType);
		}

		public SerializationHandler(ISerializer serializer)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.serializer = serializer;
		}

		private readonly ISerializer serializer;
	}
}