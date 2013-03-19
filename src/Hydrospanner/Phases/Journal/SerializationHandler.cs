namespace Hydrospanner.Phases.Journal
{
	using Disruptor;
	using Serialization;

	public sealed class SerializationHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			data.Serialize(this.serializer);
		}

		public SerializationHandler(ISerializer serializer)
		{
			this.serializer = serializer;
		}

		private readonly ISerializer serializer;
	}
}