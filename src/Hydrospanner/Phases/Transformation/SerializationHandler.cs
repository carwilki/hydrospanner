namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using log4net;
	using Serialization;

	public sealed class SerializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			if (this.mod > 1 && sequence % this.mod != this.remainder)
				return;

			if (sequence == 0)
				Log.Info("Starting deserialization");

			data.Deserialize(this.serializer);

			if (data.Body == null || data.IsTransient || data.MessageSequence > 0)
				return;

			if (this.transientTypes.Contains(data.Body.GetType()))
				data.MarkAsTransientMessage();
		}

		public SerializationHandler(ISerializer serializer, HashSet<Type> transientTypes) : this(serializer, transientTypes, 1, 0)
		{
			this.serializer = serializer;
		}
		public SerializationHandler(ISerializer serializer,  HashSet<Type> transientTypes, int mod, int remainder)
		{
			this.serializer = serializer;
			this.transientTypes = transientTypes;
			this.mod = (byte)mod;
			this.remainder = (byte)remainder;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SerializationHandler));
		private readonly ISerializer serializer;
		private readonly HashSet<Type> transientTypes;
		private readonly byte mod;
		private readonly byte remainder;
	}
}