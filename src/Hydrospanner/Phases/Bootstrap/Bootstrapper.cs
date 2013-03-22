namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Journal;
	using Messaging;
	using Snapshot;
	using Transformation;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
//			this.journalDisruptor = this.disruptors.CreateJournalDisruptor();
//			this.snapshotDisruptor = this.disruptors.CreateSnapshotDisruptor();
//			this.transformationDisruptor = this.disruptors.CreateTransformationDisruptor();

//			var info = this.persistence.Restore();
//			info = this.snapshots.RestoreSnapshots(info, this.repository);

//			this.journalDisruptor.Start();
//			this.snapshotDisruptor.Start();
//			this.messages.Restore(info, this.snapshotDisruptor, this.journalDisruptor, this.repository);

//			this.transformationDisruptor.Start();

			this.listener = this.messaging.CreateMessageListener(this.transformationDisruptor.RingBuffer);
		}

		public Bootstrapper(
			IRepository repository,
			DisruptorFactory disruptors,
			PersistenceBootstrapper persistence,
			SnapshotBootstrapper snapshots,
			MessageBootstrapper messages,
			MessagingFactory messaging)
		{
			if (repository == null) throw new ArgumentNullException("repository");
			if (disruptors == null) throw new ArgumentNullException("disruptors");
			if (persistence == null) throw new ArgumentNullException("persistence");
			if (snapshots == null) throw new ArgumentNullException("snapshots");
			if (messages == null) throw new ArgumentNullException("messages");
			if (messaging == null) throw new ArgumentNullException("messaging");

			this.repository = repository;
			this.disruptors = disruptors;
			this.persistence = persistence;
			this.snapshots = snapshots;
			this.messages = messages;
			this.messaging = messaging;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			////if (!disposing || this.disposed)
			////	return;
			////this.started = false;
			////this.disposed = true;

			////this.listener = this.listener.TryDispose();
			////TimeSpan.FromSeconds(3).Sleep();
			////this.transformationDisruptor = this.transformationDisruptor.TryDispose();
			////TimeSpan.FromMilliseconds(500).Sleep();
			////this.snapshotDisruptor = this.snapshotDisruptor.TryDispose();
			////this.journalDisruptor = this.journalDisruptor.TryDispose();
		}
		
		private readonly IRepository repository;
		private readonly DisruptorFactory disruptors;
		private readonly PersistenceBootstrapper persistence;
		private readonly SnapshotBootstrapper snapshots;
		private readonly MessageBootstrapper messages;
		private readonly MessagingFactory messaging;
		private bool started;
		private bool disposed;
		private IDisruptor<JournalItem> journalDisruptor;
		private IDisruptor<SnapshotItem> snapshotDisruptor;
		private IDisruptor<TransformationItem> transformationDisruptor;
		private MessageListener listener;
	}
}