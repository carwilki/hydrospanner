namespace Hydrospanner.Phases.Journal
{
	using Disruptor;
	using log4net;
	using Serialization;

	public sealed class SerializationHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Serializing JournalItem of type {0}.", data.SerializedType);

			data.Serialize(this.serializer);
		}

		public SerializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SerializationHandler));
		private readonly ISerializer serializer;
	}
}