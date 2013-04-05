namespace Hydrospanner.Wireup
{
	using System;
	using log4net;
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

			Log.Info("Loading mementos from latest snapshot.");

			info = this.snapshots.RestoreSnapshots(info, this.repository);

			this.snapshotDisruptor = this.disruptors.CreateSnapshotDisruptor();
			this.snapshotDisruptor.Start();

			this.journalDisruptor = this.disruptors.CreateJournalDisruptor(info);
			this.journalDisruptor.Start();

			Log.Info("Loading messages from checkpoints.");

			this.messages.Restore(info, this.journalDisruptor, this.repository);
			
			this.transformationDisruptor = this.disruptors.CreateTransformationDisruptor();
			this.transformationDisruptor.Start();

			this.listener = this.messaging.CreateMessageListener(this.transformationDisruptor.RingBuffer);
			
			Log.Debug("Attempting to start message listener.");
			this.listener.Start();

			this.started = true;

			Log.Debug("Bootstrap process complete.");
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

		private static readonly ILog Log = LogManager.GetLogger(typeof(Bootstrapper));
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