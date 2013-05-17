namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using log4net;

	public sealed class MementoHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			if (data.Memento == null)
				return; // TODO: mementos can soon be null

			Log.DebugFormat("Restoring memento of type {0}.", data.SerializedType);

			this.repository.Restore(data.Memento);
		}

		public MementoHandler(IRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			this.repository = repository;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(MementoHandler));
		private readonly IRepository repository;
	}
}