namespace Hydrospanner.Wireup
{
	using System;
	using Persistence;
	using Phases.Journal;
	using Phases.Snapshot;
	using Phases.Transformation;

	public class Bootstrapper : IDisposable
	{
		public void Start(BootstrapInfo info)
		{
			if (this.started)
				return;

			info = this.snapshots.RestoreSnapshots(info, this.repository);
			
			this.journalDisruptor = this.disruptors.CreateJournalDisruptor(info);
			this.snapshotDisruptor = this.disruptors.CreateSnapshotDisruptor();

			this.messages.Restore(info, this.journalDisruptor, this.repository);
			this.transformationDisruptor = this.disruptors.CreateTransformationDisruptor();

			this.journalDisruptor.Start();
			this.snapshotDisruptor.Start();

			this.transformationDisruptor.Start();

			this.listener = this.messaging.CreateMessageListener(this.transformationDisruptor.RingBuffer);
			this.listener.Start();

			this.started = true;
		}

		public Bootstrapper(
			IRepository repository,
			DisruptorFactory disruptors,
			SnapshotBootstrapper snapshots,
			MessageBootstrapper messages,
			MessagingFactory messaging)
		{
			if (repository == null) throw new ArgumentNullException("repository");
			if (disruptors == null) throw new ArgumentNullException("disruptors");
			if (snapshots == null) throw new ArgumentNullException("snapshots");
			if (messages == null) throw new ArgumentNullException("messages");
			if (messaging == null) throw new ArgumentNullException("messaging");

			this.repository = repository;
			this.disruptors = disruptors;
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