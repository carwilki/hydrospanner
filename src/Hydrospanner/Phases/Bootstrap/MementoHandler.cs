namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor;
	using log4net;

	public sealed class MementoHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			Log.DebugFormat("Restoring memento of type {0}.", data.SerializedType);
			this.repository.Restore(data.Key, data.MementoType, data.Memento);
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