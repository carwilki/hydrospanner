namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using log4net;
	using Serialization;

	public class SerializationHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Deserializing bootstrap item of type {0}.", data.SerializedType);

			data.Memento = this.serializer.Deserialize(data.SerializedMemento, data.SerializedType);
		}

		public SerializationHandler(ISerializer serializer)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.serializer = serializer;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SerializationHandler));
		private readonly ISerializer serializer;
	}
}