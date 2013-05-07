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

			var action = data.ItemActions;
			if (action.HasFlag(JournalItemAction.Dispatch) || action.HasFlag(JournalItemAction.Journal))
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