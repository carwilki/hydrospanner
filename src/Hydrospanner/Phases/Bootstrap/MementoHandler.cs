namespace Hydrospanner.Phases.Bootstrap
{
	using Disruptor;

	public class MementoHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			this.repository.Restore(data.Memento); // TODO: under test
		}

		public MementoHandler(IRepository repository)
		{
			this.repository = repository;
		}

		private readonly IRepository repository;
	}
}