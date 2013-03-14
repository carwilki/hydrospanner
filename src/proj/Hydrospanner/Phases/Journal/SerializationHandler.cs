namespace Hydrospanner.Phases.Journal
{
	using Disruptor;
	using Serialization;

	public class SerializationHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			data.Serialize(this.serializer);
		}

		public SerializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly ISerializer serializer;
	}
}