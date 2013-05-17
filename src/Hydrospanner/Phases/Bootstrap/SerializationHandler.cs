namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Runtime.Serialization;
	using Disruptor;
	using log4net;
	using Serialization;

	public sealed class SerializationHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			if (this.mod > 1 && sequence % this.mod != this.remainder)
				return;

			if (sequence % 10000 == 0)
				Log.InfoFormat("Deserializing memento {0}", sequence);

			Log.DebugFormat("Deserializing bootstrap item of type {0}.", data.SerializedType);

			try
			{
				Type type;
				data.Memento = this.serializer.Deserialize(data.SerializedMemento, data.SerializedType, out type);
				data.MementoType = type;
			}
			catch (SerializationException e)
			{
				Log.Fatal("Unable to deserialize a memento of type '{0}'".FormatWith(data.SerializedType), e);
				data.Memento = null;
			}
		}

		public SerializationHandler(ISerializer serializer, int mod = 1, int remainder = 0)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.serializer = serializer;
			this.mod = (byte)mod;
			this.remainder = (byte)remainder;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SerializationHandler));
		private readonly ISerializer serializer;
		private readonly byte mod;
		private readonly byte remainder;
	}
}