namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Journal;
	using Snapshot;
	using Transformation;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
			if (this.started)
				return;

			var info = this.persistence.Restore();
			info = this.snapshots.RestoreSnapshots(info, this.repository);
			
			this.journalDisruptor = this.disruptors.CreateJournalDisruptor(info);
			this.snapshotDisruptor = this.disruptors.CreateSnapshotDisruptor();
			this.transformationDisruptor = this.disruptors.CreateTransformationDisruptor();

			this.journalDisruptor.Start();
			this.snapshotDisruptor.Start();
			this.messages.Restore(info, this.snapshotDisruptor, this.journalDisruptor, this.repository);

			this.transformationDisruptor.Start();

			this.listener = this.messaging.CreateMessageListener(this.transformationDisruptor.RingBuffer);
			
			this.started = true;
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
			if (!disposing || !this.started)
				return;
			
			this.listener = this.listener.TryDispose();
			TimeSpan.FromSeconds(3).Sleep();
			this.transformationDisruptor = this.transformationDisruptor.TryDispose();
			TimeSpan.FromMilliseconds(500).Sleep();
			this.snapshotDisruptor = this.snapshotDisruptor.TryDispose();
			this.journalDisruptor = this.journalDisruptor.TryDispose();
			
			this.started = false;
		}
		
		private readonly IRepository repository;
		private readonly DisruptorFactory disruptors;
		private readonly PersistenceBootstrapper persistence;
		private readonly SnapshotBootstrapper snapshots;
		private readonly MessageBootstrapper messages;
		private readonly MessagingFactory messaging;
		private IDisruptor<JournalItem> journalDisruptor;
		private IDisruptor<SnapshotItem> snapshotDisruptor;
		private IDisruptor<TransformationItem> transformationDisruptor;
		private MessageListener listener;
		private bool started;
	}
}