namespace Hydrospanner.Phases.Transformation
{
	using Disruptor;
	using Serialization;

	public sealed class DeserializationHandler : IEventHandler<TransformationItem>
	{
		public void OnNext(TransformationItem data, long sequence, bool endOfBatch)
		{
			if (this.mod > 1 && sequence % this.mod != this.remainder)
				return;

			data.Deserialize(this.serializer);
		}

		public DeserializationHandler(ISerializer serializer) : this(serializer, 1, 0)
		{
			this.serializer = serializer;
		}
		public DeserializationHandler(ISerializer serializer, int mod, int remainder)
		{
			this.serializer = serializer;
			this.mod = (byte)mod;
			this.remainder = (byte)remainder;
		}

		private readonly ISerializer serializer;
		private readonly byte mod;
		private readonly byte remainder;
	}
}