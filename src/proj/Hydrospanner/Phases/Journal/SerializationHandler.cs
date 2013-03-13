namespace Hydrospanner.Phases.Journal
{
	using Disruptor;
	using Hydrospanner.Serialization;

	public class SerializationHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			// TODO: test
		}

		public SerializationHandler(JsonSerializer serializer)
		{
			this.serializer = serializer;
		}

		readonly JsonSerializer serializer;
	}
}