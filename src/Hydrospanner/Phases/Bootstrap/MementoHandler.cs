namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;

	public class MementoHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			this.repository.Restore(data.Memento);
		}

		public MementoHandler(IRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			this.repository = repository;
		}

		private readonly IRepository repository;
	}
}